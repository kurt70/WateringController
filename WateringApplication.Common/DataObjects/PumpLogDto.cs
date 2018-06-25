using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WateringApplication.Common.DataObjects
{
    public class PumpLogDto:PumpProgramDto
    {
        public bool Success { get; set; }
        public string ResultDescription { get; set; }
    }
}
