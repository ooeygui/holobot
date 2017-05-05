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
        private CameraHandler _handler = new CameraHandler();

        private DispatcherTimer refresh = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1.0 / 30.0) };


        public MainPage()
        {
            this.InitializeComponent();
            _dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await robot.ConnectToArduino();
            httpServer = new RobotHttpServer(3000, robot);
            await httpServer.StartServer();

            try
            {
                await _handler.initialize(CapturePreviewLeft, this);
            }
            catch (Exception ex)
            {
                // sometimes...
                Debug.WriteLine(ex.ToString());
            }

            refresh.Tick += Refresh_Tick;
            refresh.Start();
        }

        private void Refresh_Tick(object sender, object e)
        {
            refreshElement.Visibility = refreshElement.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        internal async void ImageClassified(string classification)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Status.Text = "Found : " + classification;
                
            });
        }
    }
}
