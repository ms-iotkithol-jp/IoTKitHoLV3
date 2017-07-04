using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
        bool running = false;
        //bool ledToggle = false;

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
            //tbDeviceId.Text = deviceId.ToString();
            tbSensorStatus.Text = deviceId.ToString() + (running ? " Running" : " Stopped");
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

            sensor = await TISensorTagLibrary.BLETISensor.Sensor(sensorTag);
            sensor.Initialize();
            sensor.AccelRange = accelRange;
            sensor.TemparetureScale = tempScale;
            tbSensorType.Text = sensorTag.ToString() + ":" + sensor.ManifactureName + "," + sensor.FirmwareRevision;

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

            if (!sensor.InitializedStatus)
            {
                Debug.Write("Waiting for initialize...");
                return;
            }

            if (sensor.ConnectionStatus != null)
            {
                tbConnStatus.Text = sensor.ConnectionStatus;
            }

            if (sensor == null)
            {
                // Sensors are not ready.
                return;
            }

            if (sensor.deviceNameValue != null && sensor.deviceNameValue.Value != null)
            {
                if (!running)
                {
                    string decodedDeviceName = sensor.deviceNameValue.Value.DecodeUtf8String();
                    Debug.WriteLine("***Device connected:" + decodedDeviceName);
                    sensor.ConnectionStatus = "Connected to " + decodedDeviceName;
                    tbConnStatus.Text = sensor.ConnectionStatus;
                }
            }
            else
            {
                //Sensors are still not ready.
                return;
            }
            if (sensor.systemIdValue != null && sensor.systemIdValue.Value != null)
            {
                if (!running)
                {
                    string id = IBufferExtensions.FormatID(sensor.systemIdValue.Value.DecodeUint40());
                    string idx = String.Format("{0:X16}", sensor.systemIdValue.Value.DecodeUint40());
                    Debug.WriteLine("***SystemID:" + idx + "=>" + id);
                    tbDeviceId.Text = id;
                    tbSensorStatus.Text = "Running";
                }
            }
            else
            {
                // Sensors are still not ready.
                return;
            }
            running = true;

            string tHum, tLight, tPress, tATemp, tOTemp;
            lock (this)
            {
                if (sensorReading.ATemperature != 0 && sensorReading.OTemperature != 0 && sensorReading.Pressure != 0)
                {
                    lastSensorReading.Add(new SensorReadingBuffer()
                    {
                        AccelX = sensorReading.AccelX,
                        AccelY = sensorReading.AccelY,
                        AccelZ = sensorReading.AccelZ,
                        GyroX = sensorReading.GyroX,
                        GyroY = sensorReading.GyroY,
                        GyroZ = sensorReading.GyroZ,
                        MagX = sensorReading.MagX,
                        MagY = sensorReading.MagY,
                        MagZ = sensorReading.MagZ,
                        Battery = (float) sensorReading.BatteryLevel,
                        ATemperature = (float) sensorReading.ATemperature,
                        Humidity = (float) sensorReading.Humidity,
                        Light = (float) sensorReading.Lightness,
                        Pressure = (float) sensorReading.Pressure,
                        LeftKey = sensorReading.LeftKey,
                        RightKey = sensorReading.RightKey,
                        Timestamp = timestamp
                    });
                }
                tbAccel.Text = "A=" + sensorReading.AccelX.ToString("F")
                    + "," + sensorReading.AccelY.ToString("F")
                    + "," + sensorReading.AccelZ.ToString("F");
                tbAccel.Text += ",G=" + sensorReading.GyroX.ToString("F")
                    + "," + sensorReading.GyroY.ToString("F")
                    + "," + sensorReading.GyroZ.ToString("F");
                tbAccel.Text += ",M=" + sensorReading.MagX.ToString("F")
                    + "," + sensorReading.MagY.ToString("F")
                    + "," + sensorReading.MagZ.ToString("F");
                tbHum.Text = (tHum = sensorReading.Humidity.ToString("F")) + " %";
                tbLight.Text = (tLight = sensorReading.Lightness.ToString("F")) + " lux";
                tbPress.Text = (tPress = sensorReading.Pressure.ToString("F")) + " hPa";
                tbTemp.Text = "Amb=" + (tATemp = sensorReading.ATemperature.ToString("F")) + " ℃ ";
                tbTemp.Text += ",Obj=" + (tOTemp = sensorReading.OTemperature.ToString("F")) + " ℃";

                tbSensorStatus.Text = deviceId.ToString()
                    + " Battery:" + sensorReading.BatteryLevel.ToString("F0") + "%"
                    + (running ? " Running" : " Stopped");

                tbSwitch.Text = "Left=" + sensorReading.LeftKey + ",Right=" + sensorReading.RightKey;
            }
            Debug.WriteLine("[" + timestamp.ToString("yyyyMMdd-hhmmss.fff") + "]T="
                + tATemp + "," + tOTemp + ",P=" + tPress
                + ",H=" + tHum
                + ",L=" + tLight
                + "," + tbAccel.Text
                + "," + tbSwitch.Text
                + ",B=" + sensorReading.BatteryLevel.ToString("F0") + "%"
                );

            //ledToggle = !ledToggle;
            //sensor.WriteValue(new byte[] { (byte) (ledToggle ? 3 : 0) });
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
            public float ATemperature { get; set; }
            public float OTemperature { get; set; }
            public float HTemperature { get; set; }
            public float PTemperature { get; set; }
            public float Humidity { get; set; }
            public float Light { get; set; }
            public float Pressure { get; set; }
            public float AccelX { get; set; }
            public float AccelY { get; set; }
            public float AccelZ { get; set; }
            public float GyroX { get; set; }
            public float GyroY { get; set; }
            public float GyroZ { get; set; }
            public float MagX { get; set; }
            public float MagY { get; set; }
            public float MagZ { get; set; }
            public float Battery { get; set; }
            public bool LeftKey { get; set; }
            public bool RightKey { get; set; }
            public DateTime Timestamp { get; set; }
        }

        bool ledRed;
        bool ledGreen;
        bool buzzer;

        Windows.UI.Xaml.Media.Brush ledRedDefault;
        Windows.UI.Xaml.Media.Brush ledGreenDefault;
        Windows.UI.Xaml.Media.Brush buzzerDefault;

        byte ioStatus;    // Current Status
        byte bitmask = 0; // Work bit

        private void buttonRed_Click(object sender, RoutedEventArgs e)
        {
            bitmask = 0x01;
            ledRed = !ledRed;
            if (ledRed)
            {
                ledRedDefault = buttonRed.Background;
                buttonRed.Background = new SolidColorBrush(Colors.Pink);
                ioStatus |= bitmask;
            }
            else
            {
                buttonRed.Background = ledRedDefault;
                ioStatus &= (byte) ~bitmask;
            }
            sensor.WriteValue(new byte[] { ioStatus &= 0x07 });
        }

        private void buttonGreen_Click(object sender, RoutedEventArgs e)
        {
            bitmask = 0x02;
            ledGreen = !ledGreen;
            if (ledGreen)
            {
                ledGreenDefault = buttonGreen.Background;
                buttonGreen.Background = new SolidColorBrush(Colors.YellowGreen);
                ioStatus |= bitmask;
            }
            else
            {
                buttonGreen.Background = ledGreenDefault;
                ioStatus &= (byte)~bitmask;
            }
            sensor.WriteValue(new byte[] { ioStatus &= 0x07 });
        }

        private void buttonBuzzer_Click(object sender, RoutedEventArgs e)
        {
            bitmask = 0x04;
            buzzer = !buzzer;
            ledGreen = !ledGreen;
            if (ledGreen)
            {
                buzzerDefault = buttonBuzzer.Background;
                buttonBuzzer.Background = new SolidColorBrush(Colors.Yellow);
                ioStatus |= bitmask;
            }
            else
            {
                buttonBuzzer.Background = buzzerDefault;
                ioStatus &= (byte)~bitmask;
            }
            sensor.WriteValue(new byte[] { ioStatus &= 0x07 });
        }
    }

    static class IBufferExtensions
    {
        public static string DecodeUtf8String(this Windows.Storage.Streams.IBuffer buffer)
        {
            var data = buffer.ToArray();
            return System.Text.Encoding.UTF8.GetString(data);
        }

        public static ulong DecodeUint40(this Windows.Storage.Streams.IBuffer buffer)
        {
            var data = buffer.ToArray();
            var decoded = data.Aggregate(0ul, (l, r) => (l << 8) | r);

            return decoded;
        }
        public static string FormatID(ulong ulID)
        {
            string s = String.Format("{0:X16}", ulID);
            int last = 14;
            string id = "";

            for (int i = last; i > 0; i -= 2)
            {
                id += s.Substring(i, 2) + ":";
            }
            id += s.Substring(0, 2);
            return id;
        }
    }
}
