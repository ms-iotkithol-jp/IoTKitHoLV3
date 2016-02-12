using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using GIS = GHIElectronics.UWP.Shields;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 を参照してください

namespace RPi2FHIoTHubApp
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        GIS.FEZHAT fezHat;
        int measureIntervalMSec = 1000;
        DispatcherTimer measureTimer;

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (Guid.Empty == deviceID)
            {
                FixDeviceId();
                if (deviceId == "minwinpc")
                {
                    Debug.Write("Please set deviceID or unique machine name");
                    throw new ArgumentOutOfRangeException("Please set deviceID or unique machine name");
                }
            }
            else
            {
                deviceId = deviceID.ToString();
            }
            tbDeviceId.Text = deviceId.ToString();
            fezHat = await GIS.FEZHAT.CreateAsync();
            fezHat.D2.TurnOff();
            fezHat.D3.TurnOff();
            try
            {
                var result = await TryConnect();
                if (result)
                {
                    await InitializeUpload();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            lastSensorReading = new List<SensorReadingBuffer>();
            measureTimer = new DispatcherTimer();
            measureTimer.Interval = TimeSpan.FromMilliseconds(measureIntervalMSec);
            measureTimer.Tick += MeasureTimer_Tick;
            measureTimer.Start();
        }

        List<SensorReadingBuffer> lastSensorReading;

        private void MeasureTimer_Tick(object sender, object e)
        {
            double x, y, z;
            fezHat.GetAcceleration(out x, out y, out z);
            double temp = fezHat.GetTemperature();
            double brightness = fezHat.GetLightLevel();
            var timestamp = DateTime.Now;
            lock (this)
            {
                lastSensorReading.Add(new SensorReadingBuffer()
                {
                    AccelX = x,
                    AccelY = y,
                    AccelZ = z,
                    Temperature = temp,
                    Brightness = brightness,
                    Timestamp = timestamp
                });
            }
            Debug.WriteLine("[" + timestamp.ToString("yyyyMMdd-hhmmss.fff") + "]T=" + temp + ",B=" + brightness + ",AccelX=" + x + ",AccelY=" + y + ",AccelZ=" + z);
        }

        private void FixDeviceId()
        {
            foreach (var hn in Windows.Networking.Connectivity.NetworkInformation.GetHostNames())
            {
                IPAddress ipAddr;
                if (!hn.DisplayName.EndsWith(".local") && !IPAddress.TryParse(hn.DisplayName, out ipAddr))
                {
                    deviceId = hn.DisplayName;
                    break;
                }
            }
        }

        class SensorReadingBuffer
        {
            public double Temperature { get; set; }
            public double Brightness { get; set; }
            public double AccelX { get; set; }
            public double AccelY { get; set; }
            public double AccelZ { get; set; }
            public DateTime Timestamp { get; set; }
        }

    }
}
