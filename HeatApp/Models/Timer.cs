using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeatApp.Models
{
    public class Timer
    {
        public int Addr { get; set; }
        public int TimeTableId { get; set; }
        public DateTime Time { get; set; }
        public int Idx { get; set; }
        public int Value { get; set; }
    }
}
