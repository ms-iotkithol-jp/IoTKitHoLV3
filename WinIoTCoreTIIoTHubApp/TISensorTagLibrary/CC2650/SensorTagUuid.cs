using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TISensorTagLibrary.CC2650
{
    public class SensorTagUuid
    {
        public const string UUID_INF_SERV = "0000180a-0000-1000-8000-00805f9b34fb";
        public const string UUID_INF_SYSID = "00002A23-0000-1000-8000-00805f9b34fb";
        public const string UUID_INF_MODEL_NR = "00002A24-0000-1000-8000-00805f9b34fb";
        public const string UUID_INF_SERIAL_NR = "00002A25-0000-1000-8000-00805f9b34fb";
        public const string UUID_INF_FW_NR = "00002A26-0000-1000-8000-00805f9b34fb";
        public const string UUID_INF_HW_NR = "00002A27-0000-1000-8000-00805f9b34fb";
        public const string UUID_INF_SW_NR = "00002A28-0000-1000-8000-00805f9b34fb";
        public const string UUID_INF_MANUF_NR = "00002A29-0000-1000-8000-00805f9b34fb";
        public const string UUID_INF_CERT = "00002A2A-0000-1000-8000-00805f9b34fb";
        public const string UUID_INF_PNP_ID = "00002A50-0000-1000-8000-00805f9b34fb";

        public const string UUID_IRT_SERV = "f000aa00-0451-4000-b000-000000000000";
        public const string UUID_IRT_DATA = "f000aa01-0451-4000-b000-000000000000";
        public const string UUID_IRT_CONF = "f000aa02-0451-4000-b000-000000000000";
        public const string UUID_IRT_PERI = "f000aa03-0451-4000-b000-000000000000";

        public const string UUID_ACC_SERV = "f000aa10-0451-4000-b000-000000000000"; //not for CC2650
        public const string UUID_ACC_DATA = "f000aa11-0451-4000-b000-000000000000"; //not for CC2650
        public const string UUID_ACC_CONF = "f000aa12-0451-4000-b000-000000000000"; //not for CC2650
        public const string UUID_ACC_PERI = "f000aa13-0451-4000-b000-000000000000"; //not for CC2650

        public const string UUID_HUM_SERV = "f000aa20-0451-4000-b000-000000000000";
        public const string UUID_HUM_DATA = "f000aa21-0451-4000-b000-000000000000";
        public const string UUID_HUM_CONF = "f000aa22-0451-4000-b000-000000000000";
        public const string UUID_HUM_PERI = "f000aa23-0451-4000-b000-000000000000";

        public const string UUID_MAG_SERV = "f000aa30-0451-4000-b000-000000000000"; //not for CC2650
        public const string UUID_MAG_DATA = "f000aa31-0451-4000-b000-000000000000"; //not for CC2650
        public const string UUID_MAG_CONF = "f000aa32-0451-4000-b000-000000000000"; //not for CC2650
        public const string UUID_MAG_PERI = "f000aa33-0451-4000-b000-000000000000"; //not for CC2650

        public const string UUID_BAR_SERV = "f000aa40-0451-4000-b000-000000000000";
        public const string UUID_BAR_DATA = "f000aa41-0451-4000-b000-000000000000";
        public const string UUID_BAR_CONF = "f000aa42-0451-4000-b000-000000000000";
        public const string UUID_BAR_CALI = "f000aa43-0451-4000-b000-000000000000";
        public const string UUID_BAR_PERI = "f000aa44-0451-4000-b000-000000000000";

        public const string UUID_GYR_SERV = "f000aa50-0451-4000-b000-000000000000"; //not for CC2650
        public const string UUID_GYR_DATA = "f000aa51-0451-4000-b000-000000000000"; //not for CC2650
        public const string UUID_GYR_CONF = "f000aa52-0451-4000-b000-000000000000"; //not for CC2650
        public const string UUID_GYR_PERI = "f000aa53-0451-4000-b000-000000000000"; //not for CC2650

        public const string UUID_OPT_SERV = "f000aa70-0451-4000-b000-000000000000"; //only for CC2650
        public const string UUID_OPT_DATA = "f000aa71-0451-4000-b000-000000000000"; //only for CC2650
        public const string UUID_OPT_CONF = "f000aa72-0451-4000-b000-000000000000"; //only for CC2650
        public const string UUID_OPT_PERI = "f000aa73-0451-4000-b000-000000000000"; //only for CC2650

        public const string UUID_MOV_SERV = "f000aa80-0451-4000-b000-000000000000"; //only for CC2650
        public const string UUID_MOV_DATA = "f000aa81-0451-4000-b000-000000000000"; //only for CC2650
        public const string UUID_MOV_CONF = "f000aa82-0451-4000-b000-000000000000"; //only for CC2650
        public const string UUID_MOV_PERI = "f000aa83-0451-4000-b000-000000000000"; //only for CC2650

        public const string UUID_IO_SERV  = "f000aa64-0451-4000-b000-000000000000"; //only for CC2650
        public const string UUID_IO_DATA  = "f000aa65-0451-4000-b000-000000000000"; //only for CC2650
        public const string UUID_IO_CONF  = "f000aa66-0451-4000-b000-000000000000"; //only for CC2650

        public const string UUID_BAT_SERV = "0000180f-0000-1000-8000-00805f9b34fb"; // Service
        public const string UUID_BAT_LEVL = "00002a19-0000-1000-8000-00805f9b34fb"; // Battery Level Service

        public const string UUID_KEY_SERV = "0000ffe0-0000-1000-8000-00805f9b34fb";
        public const string UUID_KEY_DATA = "0000ffe1-0000-1000-8000-00805f9b34fb";
    }
}
