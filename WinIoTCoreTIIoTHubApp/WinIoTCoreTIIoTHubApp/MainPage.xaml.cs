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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WinIoTCoreTIIoTHubApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;

        }
        int measureIntervalMSec = 1000;
        DispatcherTimer measureTimer;

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (Guid.Empty == MSIoTKiTHoLJP.IoTHoLConfig.DeviceID)
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
                deviceId = MSIoTKiTHoLJP.IoTHoLConfig.DeviceID.ToString();
            }
            tbDeviceId.Text = deviceId.ToString();
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

            sensor = await TISensorTagLibrary.BLETISensor.Sensor(this.sensorTag);
            sensor.Initialize();
            tbSensorType.Text = this.sensorTag.ToString();

            lastSensorReading = new List<SensorReadingBuffer>();
            measureTimer = new DispatcherTimer();
            measureTimer.Interval = TimeSpan.FromMilliseconds(measureIntervalMSec);
            measureTimer.Tick += MeasureTimer_Tick;
            measureTimer.Start();
        }

        TISensorTagLibrary.BLETISensor sensor = null;
        List<SensorReadingBuffer> lastSensorReading;

        private void MeasureTimer_Tick(object sender, object e)
        {
            var sensorReading = sensor.ReadSensorValue();
            var timestamp = DateTime.Now;
            lock (this)
            {
                if (sensorReading.ATemperature != 0 && sensorReading.OTemperature != 0 && sensorReading.Pressure != 0)
                {
                    lastSensorReading.Add(new SensorReadingBuffer()
                    {
                        AccelX = sensorReading.AccelX,
                        AccelY = sensorReading.AccelY,
                        AccelZ = sensorReading.AccelZ,
                        Temperature = sensorReading.ATemperature,
                        Humidity = sensorReading.Humidity,
                        Pressure = sensorReading.Pressure,
                        LeftKey = sensorReading.LeftKey,
                        RightKey = sensorReading.RightKey,
                        Timestamp = timestamp
                    });
                }
                tbAccel.Text = "X=" + sensorReading.AccelX + ",Y=" + sensorReading.AccelY + ",Z=" + sensorReading.AccelZ;
                tbHum.Text = sensorReading.Humidity.ToString() + " %";
                tbPress.Text = sensorReading.Pressure.ToString() + " hPa";
                tbTemp.Text = sensorReading.OTemperature.ToString() + " Cercius Degree";
                tbSwitch.Text = "Left=" + sensorReading.LeftKey + ",Right=" + sensorReading.RightKey;
            }
            Debug.WriteLine("[" + timestamp.ToString("yyyyMMdd-hhmmss.fff") + "]T=" + sensorReading.ATemperature + ",P=" + sensorReading.Pressure + ",H=" + sensorReading.Humidity + ",AX=" + sensorReading.AccelX + ",AY=" + sensorReading.AccelY + ",AZ=" + sensorReading.AccelZ);
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
            public double Humidity { get; set; }
            public double Pressure { get; set; }
            public double AccelX { get; set; }
            public double AccelY { get; set; }
            public double AccelZ { get; set; }
            public bool LeftKey { get; set; }
            public bool RightKey { get; set; }
            public DateTime Timestamp { get; set; }
        }

    }
}
