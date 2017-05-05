using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.UI.Xaml.Controls;
using FrameAnalysis;

namespace Control
{
    public class CameraHandler : Bindable
    {
        private MainPage _mainPage;
        private MediaCapture _mediaCapture;
        private Image _imageElement;

        private MediaFrameReader[] _sourceReaders = new MediaFrameReader[3];
        private IReadOnlyDictionary<MediaFrameSourceKind, FrameRenderer> _frameRenderers = null;

        private int _groupSelectionIndex;
        private int _currentClassifiedImage = -1;
        private string _currentClassifiedImageName = "";

        private FrameAnalyzer _analyzer = new FrameAnalyzer();

        public CameraHandler()
        {
        }

        public int ClassifiedImage
        {
            get { return _currentClassifiedImage; }
            internal set
            {
                _currentClassifiedImage = value;
                NotifyPropertyChanged("ClassifiedImage");
                
                ClassifiedImageName = _analyzer.ClassificztionById(_currentClassifiedImage);
            }
        }

        public string ClassifiedImageName
        {
            get { return _currentClassifiedImageName;  }
            internal set
            {
                _currentClassifiedImageName = value;
                NotifyPropertyChanged("ClassifiedImageName");
            }
        }

        public async Task initialize(Image imageElement, MainPage mainPage)
        {
            _analyzer.Init();

            _mainPage = mainPage;

            if (_frameRenderers == null)
            {
                _frameRenderers = new Dictionary<MediaFrameSourceKind, FrameRenderer>()
                {
                    { MediaFrameSourceKind.Color, new FrameRenderer(imageElement) },
                };
            }

            await CleanupMediaCaptureAsync();

            _imageElement = imageElement;

            // Identify the color, depth, and infrared sources of each group,
            // and keep only the groups that have at least one recognized source.
            var allGroups = await MediaFrameSourceGroup.FindAllAsync();
            var eligibleGroups = allGroups.Select(g => new
            {
                Group = g,

                // For each source kind, find the source which offers that kind of media frame,
                // or null if there is no such source.
                SourceInfos = new MediaFrameSourceInfo[]
                {
                    g.SourceInfos.FirstOrDefault(info => info.SourceKind == MediaFrameSourceKind.Color)
                }
            }).Where(g => g.SourceInfos.Any(info => info != null)).ToList();

            if (eligibleGroups.Count == 0)
            {
                return;
            }

            bool foundOne = false;
            int count = eligibleGroups.Count;
            MediaFrameSourceInfo[] sourceInfos = null;
            string groupName = "";

            while (!foundOne && count-- >= 0)
            {

                // Pick next group in the array after each time the Next button is clicked.
                _groupSelectionIndex = (_groupSelectionIndex + 1) % eligibleGroups.Count;
                var selected = eligibleGroups[_groupSelectionIndex];

                try
                {
                    // Initialize MediaCapture with selected group.
                    // This can raise an exception if the source no longer exists,
                    // or if the source could not be initialized.
                    await InitializeMediaCaptureAsync(selected.Group);

                    groupName = selected.Group.DisplayName;
                    sourceInfos = selected.SourceInfos;
                    foundOne = true;
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception.Message + "\n" + exception.StackTrace);
                    await CleanupMediaCaptureAsync();
                }
            }
            if (!foundOne)
            {
                return;
            }

            // Set up frame readers, register event handlers and start streaming.
            for (int i = 0; i < sourceInfos.Length; i++)
            {
                MediaFrameSourceInfo info = sourceInfos[i];
                if (info != null)
                {
                    // Access the initialized frame source by looking up the the ID of the source found above.
                    // Verify that the Id is present, because it may have left the group while were were
                    // busy deciding which group to use.
                    MediaFrameSource frameSource = null;
                    if (_mediaCapture.FrameSources.TryGetValue(info.Id, out frameSource))
                    {
                        MediaFrameReader frameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource);
                        frameReader.FrameArrived += FrameReader_FrameArrived;

                        MediaFrameReaderStartStatus status = await frameReader.StartAsync();
                        if (status != MediaFrameReaderStartStatus.Success)
                        {
                            Debug.WriteLine($"Unable to start {info.SourceKind} reader. Error: {status}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Unable to start {info.SourceKind} reader. Frame source not found");
                    }
                }
                else
                {
                    string frameKind = (i == 0 ? "Color" : i == 1 ? "Depth" : "Infrared");
                    Debug.WriteLine($"No {frameKind} source in group '{groupName}'.");
                }
            }
        }

        private async Task InitializeMediaCaptureAsync(MediaFrameSourceGroup sourceGroup)
        {
            if (_mediaCapture != null)
            {
                return;
            }

            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            if (allVideoDevices.Count > 0)
            {
                var cameraDevice = allVideoDevices[0];

                // Initialize mediacapture with the source group.
                _mediaCapture = new MediaCapture();
                var settings = new MediaCaptureInitializationSettings
                {
                    VideoDeviceId = cameraDevice.Id,

                    SourceGroup = sourceGroup,

                    // This media capture can share streaming with other apps.
                    SharingMode = MediaCaptureSharingMode.ExclusiveControl,

                    // Only stream video and don't initialize audio capture devices.
                    StreamingCaptureMode = StreamingCaptureMode.Video,

                    // Set to CPU to ensure frames always contain CPU SoftwareBitmap images
                    // instead of preferring GPU D3DSurface images.
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu
                };

                await _mediaCapture.InitializeAsync(settings);

                var cameraProperties = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview).Select(x => x as VideoEncodingProperties).ToList();

                foreach (var mediaEncodingProperty in cameraProperties)
                {
                    if (mediaEncodingProperty.Width == 640 &&
                        mediaEncodingProperty.Height == 480 &&
                        mediaEncodingProperty.FrameRate.Numerator == 15 &&
                        string.Compare(mediaEncodingProperty.Subtype, "YUY2") == 0)
                    {
                        Debug.WriteLine("Chosen: " + mediaEncodingProperty.Width + "x" + mediaEncodingProperty.Height + " FPS: " + mediaEncodingProperty.FrameRate.Numerator + "Type:" + mediaEncodingProperty.Type + "   SubType:" + mediaEncodingProperty.Subtype);
                        await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, mediaEncodingProperty);
                        break;
                    }
                }
            }
        }

        private async Task CleanupMediaCaptureAsync()
        {
            if (_mediaCapture != null)
            {
                using (var mediaCapture = _mediaCapture)
                {
                    _mediaCapture = null;

                    foreach (var reader in _sourceReaders)
                    {
                        if (reader != null)
                        {
                            reader.FrameArrived -= FrameReader_FrameArrived;
                            await reader.StopAsync();
                            reader.Dispose();
                        }
                    }
                }
            }
        }

        private void FrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            string classification = string.Empty;
            // TryAcquireLatestFrame will return the latest frame that has not yet been acquired.
            // This can return null if there is no such frame, or if the reader is not in the
            // "Started" state. The latter can occur if a FrameArrived event was in flight
            // when the reader was stopped.
            using (var frame = sender.TryAcquireLatestFrame())
            {
                if (frame != null)
                {
                    var renderer = _frameRenderers[frame.SourceKind];

                    using (var inputBitmap = frame?.VideoMediaFrame.SoftwareBitmap)
                    {
                        if (inputBitmap != null)
                        {
                            using (var buffer = inputBitmap.LockBuffer(Windows.Graphics.Imaging.BitmapBufferAccessMode.Read))
                            {
                                int classificationId = _analyzer.BeginProcessing(buffer, buffer.GetPlaneDescription(0).Stride, inputBitmap.PixelWidth, inputBitmap.PixelHeight);
                                if (classificationId != -1)
                                {
                                    ClassifiedImage = classificationId;
                                }
                            }
                        }

                        renderer.ProcessFrame(frame);
                    }

                }
            }
        }
    }


}
