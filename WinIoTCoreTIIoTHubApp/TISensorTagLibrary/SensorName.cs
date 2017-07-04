using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TISensorTagLibrary
{
    public enum SensorTag
    {
        CC2541,
        CC2650
    }


    public enum SensorName
    {
        Accelerometer,
        Gyroscope,
        HumiditySensor,
        LightSensor,
        TemperatureSensor,
        Magnetometer,
        PressureSensor,
        BatteryLevel,
        SimpleKeyService,
        IOSerivice
    }

    public enum TemperatureScale
    {
        Celsius,
        Farenheit
    }

    public enum AccelRange
    {
        //http://processors.wiki.ti.com/index.php/CC2650_SensorTag_User's_Guide#Movement_Sensor
        _2G = 0,
        _4G = 1,
        _8G = 2,
        _16G = 3
    }
}
