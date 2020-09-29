using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HeatApp.Models
{
    public class ValveView : Valve
    {
        public ValveView()
        {
        }

        public ValveView(Valve valve, ValveLog lastLog, TimeTable timeTable)
        {
            Actual = lastLog != null ? lastLog.Actual : 0;
            Addr = valve.Addr;
            Auto = lastLog != null ? lastLog.Auto : false;
            Battery = lastLog != null ? lastLog.Battery : 0;
            BoilerEnabled = valve.BoilerEnabled;
            Caption = valve.Caption;
            Error = lastLog != null ? lastLog.Error : 0;
            Locked = lastLog != null ? lastLog.Locked : false;
            Time = lastLog != null ? lastLog.Time : new DateTime();
            TimeTable = valve.TimeTable;
            Turn = lastLog != null ? lastLog.Turn : 0;
            Wanted = lastLog != null ? lastLog.Wanted : 0;
            Window = lastLog != null ? lastLog.Window : false;
            if (timeTable != null && timeTable.Caption != null)
            {
                TimeTableCaption = timeTable.Caption;
            }
        }

        public DateTime Time { get; set; }
        public bool Auto { get; set; }
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        public decimal Actual { get; set; }
        [DisplayFormat(DataFormatString = "{0:F1}", ApplyFormatInEditMode = true)]
        public decimal Wanted { get; set; }
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        public decimal Battery { get; set; }
        public bool Window { get; set; }
        public int Turn { get; set; }
        public int Error { get; set; }
        public string TimeTableCaption { get; set; }
        public bool OnLine
        {
            get
            {
                return Time.AddSeconds(600) >= DateTime.Now;
            }
        }
    }
}
