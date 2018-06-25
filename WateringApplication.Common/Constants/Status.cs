using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WateringApplication.Common.Constants
{
    public class WorkingStatus
    {
        public enum StatusType
        {
            Disabled,
            Running,
            Pumping,
            OutOfWater
        }

        public static string GetStatusTextString(StatusType status)
        {
            switch (status)
            {
                case StatusType.Disabled:
                    return "The pump is disabled.";
                case StatusType.Running:
                    return "The pump is waiting for program.";
                case StatusType.Pumping:
                    return "The pump is pumping water.";
                case StatusType.OutOfWater:
                    return "The pump is out of water.";
            }
            return string.Empty;
        }
    }
}
