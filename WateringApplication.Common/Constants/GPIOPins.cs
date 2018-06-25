using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WateringApplication.Common.Constants
{
    public class GPIOPins
    {
        public const int EmptyWaterLevelPin = 08;
        public const int LowWaterLevelPin = 22;
        public const int MediumWaterLevelPin = 32;
        public const int FullWaterLevelPin = 40;
        public const int RunPumpPin = 11;
    }
}
