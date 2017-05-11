/*
    Copyright(c) Microsoft Corp. All rights reserved.
    
    The MIT License(MIT)
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files(the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions :
    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System;
using System.Diagnostics;
using System.Linq;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Control
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private HoloLensRobot robot = new HoloLensRobot();
        private RobotHttpServer httpServer;
        private CoreDispatcher _dispatcher;
        public CameraHandler cameraHandler = new CameraHandler();
        private Drive drive;

        private DispatcherTimer refresh = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1.0 / 30.0) };

        public DroneControlViewModel VM { get; set; }

        public MainPage()
        {
            this.InitializeComponent();
            _dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            //Assign View Model to Data Context
            VM = new DroneControlViewModel();
            this.DataContext = VM;
            VM.Objective = "";

            var hostNames = NetworkInformation.GetHostNames();
            var hostName = hostNames.FirstOrDefault(name => name.Type == HostNameType.DomainName)?.DisplayName ?? "???";
            VM.Title = hostName;


            //Handle Power Button
            DroneOverlay.PowerButtonClicked += delegate (object sender, EventArgs args)
            {
                //Handle the Power Button Here..
            };


            robot.handler = cameraHandler;
            cameraHandler.PropertyChanged += CameraHandler_PropertyChanged;


            await robot.ConnectToArduino();
            httpServer = new RobotHttpServer(3000, robot);
            await httpServer.StartServer();

            drive = new Drive(robot);
            drive.initialize();

            robot.PropertyChanged += Robot_PropertyChanged;

            try
            {
                await cameraHandler.initialize(FullScreenImage, PreviewImage, this);
            }
            catch (Exception ex)
            {
                // sometimes...
                Debug.WriteLine(ex.ToString());
            }

            refresh.Tick += Refresh_Tick;
            refresh.Start();
        }

        private void Robot_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.CompareOrdinal(e.PropertyName, "CurrentMission") == 0)
            {
                if (robot.CurrentMission == -1)
                {
                    VM.Objective = "";
                }
                else
                {
                    VM.Objective = $"Looking for {cameraHandler.ClassifiedImageNameFromId(robot.CurrentMission)}";
                }
            }
        }

        private void CameraHandler_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.CompareOrdinal(e.PropertyName, "ClassifiedImageName") == 0)
            {
                // Only display current mission finds.
                //if (robot.CurrentMission == cameraHandler.ClassifiedImage)
                {
                    VM.Status = $"Classifier Detected: {cameraHandler.ClassifiedImageName}";
                }
            }
        }

        private void Refresh_Tick(object sender, object e)
        {
            refreshElement.Visibility = refreshElement.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
