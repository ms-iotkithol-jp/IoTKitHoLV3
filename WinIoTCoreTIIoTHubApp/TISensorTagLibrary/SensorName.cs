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
        TemperatureSensor,
        Magnetometer,
        PressureSensor,
        SimpleKeyService
    }

    public enum TemperatureScale
    {
        Celsius,
        Farenheit
    }

}
