using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfEmuIoTHubApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            sensorDelta = new Random(DateTime.Now.Millisecond);
            lastTemperature = int.Parse(tbTT.Text);
            this.Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Guid.Empty == IoTHoLConfig.deviceId)
            {
                MessageBox.Show("Please set valid deviceId before you learn!");
                throw new ArgumentOutOfRangeException("deviceId is empty!");
            }
            txtDeviceId.Text = IoTHoLConfig.deviceId.ToString();
            txtMSEndPoint.Text = IoTHoLConfig.DeviceEntryEndPoint;

            try
            {
                var result = TryConnect();
                if (result)
                {
                    InitializeUpload();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            measureTimer = new DispatcherTimer();
            measureTimer.Interval = TimeSpan.FromMilliseconds(measureIntervalMSec);
            measureTimer.Tick += measureTimer_Tick;
            measureTimer.Start();
        }

        SensorStatus sensorStatus = SensorStatus.Still;
        void measureTimer_Tick(object sender, EventArgs e)
        {
            measureTimer.Stop();
            lock (this)
            {
                if (sensorStatus == SensorStatus.Still)
                {
                    lastAccelX = (sensorDelta.NextDouble() - 0.5) * 0.01;
                    lastAccelY = (sensorDelta.NextDouble() - 0.5) * 0.01;
                    lastAccelZ = -1 + (sensorDelta.NextDouble() - 0.5) * 0.01;
                }
                else
                {
                    lastAccelX = (sensorDelta.NextDouble() - 0.5) * 3;
                    lastAccelY = (sensorDelta.NextDouble() - 0.5) * 3;
                    lastAccelZ = -1 + (sensorDelta.NextDouble() - 0.5) * 3;
                }

                double tt = double.Parse(tbTT.Text);
                double dt = tt - lastTemperature;
                lastTemperature += dt * 0.1;
                lastTemperature += (sensorDelta.NextDouble() - 0.5) * 0.01;
            }
            txtAccelX.Text = lastAccelX.ToString();
            txtAccelY.Text = lastAccelY.ToString();
            txtAccelZ.Text = lastAccelZ.ToString();
            txtTemperature.Text = lastTemperature.ToString();
            measureTimer.Start();
        }

        Random sensorDelta;
        DispatcherTimer measureTimer;
        long measureIntervalMSec = 300;

        enum SensorStatus
        {
            Still,
            Touching
        };


        double lastTargetTemp = 0;
        private void buttonTouch_Click(object sender, RoutedEventArgs e)
        {
            if (sensorStatus == SensorStatus.Still)
            {
                buttonTouch.Content = "Touching...";
                sensorStatus = SensorStatus.Touching;
                lastTargetTemp = int.Parse(tbTT.Text);
                tbTT.Text = "34";
            }
            else
            {
                buttonTouch.Content = "Touch";
                sensorStatus = SensorStatus.Still;
                tbTT.Text = lastTargetTemp.ToString();
            }
        }

    }
}
