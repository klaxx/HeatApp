using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HeatApp.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace HeatApp.Services
{
    public class CommandService
    {
        private readonly HeatAppContext db;
        private static readonly Dictionary<string, MemoryRecord> memoryLayout = MemoryLayout.GetLayout();

        public CommandService(HeatAppContext db)
        {
            this.db = db;
        }

        public int GetQueueCount()
        {
            return db.Queue.Count();
        }

        public void GetSettings(int addr)
        {
            DateTime now = DateTime.Now;
            db.Queue.Add(new Command() { Addr = addr, Data = "Gff", Sent = 0, Time = now });
            for (var i = 0; i < 0x36; i++)
            {
                db.Queue.Add(new Command() { Addr = addr, Data = "G" + i.ToString("x02"), Sent = 0, Time = now });
            }
            db.SaveChanges();
        }

        public List<ValveView> GetValveStates()
        {
            List<ValveView> res = (from v in db.Valves
                                   join ll in db.ValveLog.GroupBy(vl => vl.Addr).Select(g => new { Addr = (int?)g.Key, Id = (int?)g.Max(p => p.Id) }) on v.Addr equals ll.Addr into lljoin
                                   from ll in lljoin.DefaultIfEmpty()
                                   join vl in db.ValveLog on ll.Id equals vl.Id into vljoin
                                   from vl in vljoin.DefaultIfEmpty()
                                   select new ValveView(v, vl, null)).ToList();
            return res;
        }

        public string GetKettleAction()
        {
            bool boilerEnabled = db.Configs.Single(b => b.ConfigID == "BoilerEnabled").ConfigValue == "on";
            if (boilerEnabled)
            {
                List<ValveView> log = GetValveStates();
                var res = log.Where(l => l.BoilerEnabled && l.OnLine);
                int onlineCount = res.Count();
                if (onlineCount == 0)
                {
                    return "termostat";
                }
                var fc = res.Where(f => f.Actual < f.Wanted && ((f.Wanted - f.Actual) > (decimal)0.25) && f.Turn > 40).Count();
                if (fc > 0)
                {
                    return "on";
                }
                return "off";
            }
            return "termostat";
        }

        public void UpdateValve(ValveView valveView)
        {
            var lastLog = db.ValveLog.Where(v => v.Addr == valveView.Addr).OrderByDescending(v => v.Time).FirstOrDefault();
            List<string> commands = new List<string>();
            if (lastLog == null)
            {
                commands.Add("A" + Convert.ToInt32(valveView.Wanted * 2).ToString("x02"));
                commands.Add(valveView.Auto ? "M01" : "M00");
            }
            else
            {
                if (lastLog.Auto != valveView.Auto)
                {
                    commands.Add(valveView.Auto ? "M01" : "M00");
                }
                if (lastLog.Wanted != valveView.Wanted)
                {
                    commands.Add("A" + Convert.ToInt32(valveView.Wanted * 2).ToString("x02"));
                }
                if (lastLog.Locked != valveView.Locked)
                {
                    commands.Add(valveView.Locked ? "L01" : "L00");
                }
            }
            if (commands.Count > 0)
            {
                SendCommands(commands, valveView.Addr);
            }
        }

        private void SetTimetableToValves(int ttid)
        {
            List<int> valves = db.Valves.Where(v => v.TimeTable == ttid).Select(t => t.Addr).ToList();
            foreach (var addr in valves)
            {
                SendTimetableToValve(addr, ttid);
            }
        }

        public void SaveTimeTable(TimeTable timeTable)
        {
            bool newTimetable = timeTable.Id == 0;
            if (newTimetable)
            {
                db.Add(timeTable);
            }
            else
            {
                db.Update(timeTable);
                var timersToDElete = db.Timers.Where(t => t.TimeTableId == timeTable.Id);
                db.Timers.RemoveRange(timersToDElete);
            }

            //db.SaveChanges();

            var timers = JsonConvert.DeserializeObject<TimerRecord[][]>(timeTable.TimersSerialized);
            for (var d = 0; d <= 7; d++)
            {
                for (var t = 0; t <= 7; t++)
                {
                    var idx = t + (d << 4);
                    var val = 4095 + (1 << 12);
                    if (d > 0 && timers[d - 1].Length > t)
                    {
                        var rr = timers[d - 1][t];
                        val = (rr.time) + (rr.temp << 12);
                    }
                    Timer tm = new Timer() { Addr = 0, TimeTableId = timeTable.Id, Time = DateTime.Now, Idx = idx, Value = val };
                    timeTable.Timers.Add(tm);
                }
            }
            db.SaveChanges();
            SetTimetableToValves(timeTable.Id);
        }



        public void SendTimetableToValve(int valve, int ttid)
        {
            //List<Timer> timers = db.Timers.Where(t => t.TimeTableId == ttid).ToList();
            TimeTable timetable = db.Timetables.Include(x => x.Timers).SingleOrDefault(tt => tt.Id == ttid);
            if (timetable == null)
            {
                return;
            }
            //teploty
            List<string> commands = new List<string>();
            commands.Add("S" + memoryLayout["temperature0"].idx.ToString("x02") + timetable.NoFrostTemp.ToString("x02"));
            commands.Add("S" + memoryLayout["temperature1"].idx.ToString("x02") + timetable.SleepTemp.ToString("x02"));
            commands.Add("S" + memoryLayout["temperature2"].idx.ToString("x02") + timetable.EcoTemp.ToString("x02"));
            commands.Add("S" + memoryLayout["temperature3"].idx.ToString("x02") + timetable.WarmTemp.ToString("x02"));
            //mod - nastavení casu na celý týden
            commands.Add("S" + memoryLayout["timer_mode"].idx.ToString("x02") + (1).ToString("x02"));
            foreach (var row in timetable.Timers)
            {
                var d = row.Idx >> 4;
                var t = row.Idx & 0xf;
                var x = row.Value;
                var h = ((x & 0xfff) / 60);
                var m = ((x & 0xfff) % 60);
                commands.Add("W" + d.ToString() + t.ToString() + (x | (h * 60 + m)).ToString("x04"));
            }
            SendCommands(commands, valve);
        }

        private void SendCommands(List<string> commands, int valve)
        {
            if (commands.Count > 0)
            {
                foreach (var cmd in commands)
                {
                    db.Queue.Add(new Command() { Addr = valve, Data = cmd, Time = DateTime.Now, Sent = 0 });
                }
                db.SaveChanges();
            }
        }


        public Dictionary<string, MemoryRecord> GetMemoryLayout
        {
            get
            {
                return memoryLayout;
            }
        }
    }
}
