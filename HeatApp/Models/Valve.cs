using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace HeatApp.Models
{
    public class Valve
    {
        [Key]
        public int Addr { get; set; }
        [MaxLength(100)]
        public string Caption { get; set; }
        public bool BoilerEnabled { get; set; }
        public int TimeTable { get; set; }
        public bool Locked { get; set; }
    }
}
