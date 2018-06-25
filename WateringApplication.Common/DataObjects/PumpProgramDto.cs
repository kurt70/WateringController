using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WateringApplication.Common.DataObjects
{

    /// <summary>
    /// represents a recurring program that is run at a given time for a given duration
    /// </summary>
    public class PumpProgramDto
    {
        //TODO: Change tihis to a Cron expression?
        public DateTime StartTime { get; set; }
        public TimeSpan RunningTime { get; set; }
    }
}
