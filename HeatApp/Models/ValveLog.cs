using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace HeatApp.Models
{
    public class ValveLog
    {
        public ValveLog() { }

        public ValveLog(int addr, StateMessage sm)
        {
            this.Addr = addr;
            this.Auto = sm.Auto;
            this.Locked = sm.Locked;
            this.Turn = sm.Valve;
            this.Wanted = sm.Wanted;
            this.Window = sm.Window;
            this.Actual = sm.Temp;
            this.Battery = sm.Battery;
            this.Error = sm.Error;
            this.Time = DateTimeOffset.FromUnixTimeSeconds(sm.LastContact).DateTime;
        }


        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int Addr { get; set; }
        public DateTime Time { get; set; }
        [MaxLength(10)]
        public bool Auto { get; set; }
        public decimal Actual { get; set; }
        public decimal Wanted { get; set; }
        public decimal Battery { get; set; }
        public bool Window { get; set; }
        public int Turn { get; set; }
        public bool Locked { get; set; }
        public int Error { get; set; }
    }
}
