using System;

using Microsoft.SPOT;
using Microsoft.SPOT.Input;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using System.Threading;

namespace PinKitIoTHubApp
{
    public partial class Program : Microsoft.SPOT.Application
    {
        public static void Main()
        {
            Program myApplication = new Program();

            Window mainWindow = myApplication.CreateWindow();

            try
            {
                myApplication.ProgramInitialize();
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

            // Start the application
            myApplication.Run(mainWindow);
        }

        private Window mainWindow;

        public Window CreateWindow()
        {
            // Create a window object and set its size to the
            // size of the display.
            mainWindow = new Window();
            mainWindow.Height = SystemMetrics.ScreenHeight;
            mainWindow.Width = SystemMetrics.ScreenWidth;

            return mainWindow;
        }

        PinKit.BoardFullColorLED.Colors blinkColor;
        bool blinking = true;
        public Program()
        {
            pinkit = new PinKit.PinKit();
            blinkColor = PinKit.BoardFullColorLED.Colors.Red;
            BlinkLED();
        }

        private void BlinkLED()
        {
            pinkitStatusLEDThread = new Thread(() =>
            {
                bool on = true;
                bool blinkingStatus;
                lock (this)
                {
                    blinkingStatus = blinking;
                }
                while (blinkingStatus)
                {
                    if (on)
                    {
                        pinkit.LED.SetColor(blinkColor);
                    }
                    else
                    {
                        pinkit.LED.SetColor(PinKit.BoardFullColorLED.Colors.Black);
                    }
                    Thread.Sleep(500);
                    lock (this)
                    {
                        blinkingStatus = blinking;
                    }
                    on = false;
                }
            });
            pinkitStatusLEDThread.Start();
        }

        PinKit.PinKit pinkit;
        Thread pinkitStatusLEDThread;
        string proxyHost = "";
        int proxyPort = 80;
        string IoTDeviceId = "";

        private bool ProgramInitialize()
        {
            bool result = true;

            var ipAddr = pinkit.SetupNetwork();
            if (ipAddr == "0.0.0.0")
            {
                result = false;
            }
            else
            {
                pinkit.SyncTimeService();
                pinkitStatusLEDThread.Suspend();
                blinkColor = PinKit.BoardFullColorLED.Colors.Green;
                pinkitStatusLEDThread.Resume();
                TryConnect();
                blinking = false;
                pinkit.LED.SetColor(PinKit.BoardFullColorLED.Colors.Blue);

                Initialize();
            }
            return result;
        }
    }
}
