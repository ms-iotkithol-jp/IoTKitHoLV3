using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TISensorTagLibrary
{
    public class SensorValueChangedEventArgs : EventArgs
    {
        public SensorValueChangedEventArgs(byte[] rawData, DateTimeOffset timestamp, SensorName origin)
        {
            RawData = rawData;
            Origin = origin;
            Timestamp = timestamp;
        }

        public SensorName Origin { get; private set; }

        public byte[] RawData { get; private set; }

        public DateTimeOffset Timestamp { get; private set; }
    }
}
