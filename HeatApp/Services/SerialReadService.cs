using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HeatApp.Models;
using Microsoft.EntityFrameworkCore;
using System.IO.Ports;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace HeatApp.Services
{
    public class SerialReadService : BackgroundService
    {
        private readonly IConfiguration configuration;
        private readonly IServiceScopeFactory scopeFactory;
        private static SerialPort serialPort;
        private static int addr = -1;

        public SerialReadService(IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            this.configuration = configuration;
            this.scopeFactory = scopeFactory;
        }


        public void Read()
        {
            while (true)
            {
                string message = serialPort.ReadLine();
                Console.WriteLine(message);
                string line = message.Trim();
                ProcesSerialData(line);
            }
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string port = configuration.GetSection("Serial").GetValue<string>("Port");
            serialPort = new SerialPort(port)
            {
                BaudRate = 38400,
                Parity = Parity.None,
                StopBits = StopBits.One,
                DataBits = 8,
                Handshake = Handshake.None
            };
            //serialPort.DataReceived += SerialPort_DataReceived;
            try
            {
                serialPort.Open();
                Thread readThread = new Thread(Read);
                readThread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return Task.CompletedTask;
        }

        private void ProcesSerialData(string line)
        {
            using (IServiceScope scope = scopeFactory.CreateScope())
            {
                HeatAppContext db = scope.ServiceProvider.GetRequiredService<HeatAppContext>();

                if (line == "")
                {
                    return;
                }
                string data = null;
                if (line.Substring(0, 1) == "(" && line.Substring(3, 1) == ")")
                {
                    addr = int.Parse(line.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
                    data = line.Substring(4);
                }
                else if (line.Substring(0, 1) == "*")
                {
                    var cq = db.Queue.OrderBy(o => o.Sent).Where(w => w.Addr == addr && w.Sent > 0).FirstOrDefault();
                    if (cq != null)
                    {
                        db.Queue.Remove(cq);
                        db.SaveChanges();
                    }
                    data = line.Substring(1);
                }
                else if (line.Substring(0, 1) == "-")
                {
                    data = line.Substring(1);
                }
                else if (line == "}")
                {
                    data = line.Substring(1);
                    addr = 0;
                }
                else
                {
                    addr = 0;
                }

                if (line == "RTC?")
                {
                    DateTime now = DateTime.Now;
                    string time = "H" + now.Hour.ToString("x02") + now.Minute.ToString("x02") + now.Second.ToString("x02") + ((int)Math.Round((decimal)now.Millisecond / 10)).ToString("x02");
                    string date = "Y" + (now.Year - 2000).ToString("x02") + now.Month.ToString("x02") + now.Day.ToString("x02");
                    serialPort.WriteLine(date);
                    serialPort.WriteLine(time);
                }
                else if ((line == "N0?") || (line == "N1?"))
                {
                    int pr = 0;
                    int[] req = { 0, 0, 0, 0 };
                    string v = "O0000";

                    var res = db.Queue.GroupBy(q => q.Addr).Select(g => new { addr = g.Key, count = g.Count() }).OrderBy(o => o.count).ToList();
                    foreach (var rec in res)
                    {
                        if (rec.addr > 0 && rec.addr < 30)
                        {
                            v = null;
                            if ((line == "N1?") && (rec.count > 20))
                            {
                                v = "O" + addr.ToString("x02") + pr.ToString("x02");
                                pr = addr;
                                continue;
                            }
                            req[(int)addr / 8] |= (int)Math.Pow(2, (addr % 8));
                        }
                    }
                    if (v == null)
                    {
                        v = "P" + req[0].ToString("x02") + req[1].ToString("x02") + req[3].ToString("x02") + req[3].ToString("x02");
                    }
                    serialPort.WriteLine(v);
                }
                else
                {
                    if (addr > 0)
                    {
                        if (data.Substring(0, 1) == "?")
                        {
                            ExecuteCommands(addr);
                        }
                        else if ((data.Substring(0, 1) == "G" || data.Substring(0, 1) == "S") && data.Substring(1, 1) == "[")
                        {
                            SaveSettings(addr, data);
                        }
                        else if ((data.Substring(0, 1) == "D" || data.Substring(0, 1) == "A") && data.Substring(1, 1) == " ")
                        {
                            SaveState(addr, data);
                        }
                    }
                }
            }
        }

        private void SaveSettings(int addr, string data)
        {
            using (IServiceScope scope = scopeFactory.CreateScope())
            {
                HeatAppContext db = scope.ServiceProvider.GetRequiredService<HeatAppContext>();

                int idx = int.Parse(data.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                int value = int.Parse(data.Substring(6), System.Globalization.NumberStyles.HexNumber);
                string cmd = data.Substring(0, 1);
                if (cmd == "G" || cmd == "S")
                {
                    Setting memRec = db.ValveSettings.SingleOrDefault(m => m.Addr == addr && m.Idx == idx);
                    if (memRec != null)
                    {
                        memRec.Time = DateTime.Now;
                        memRec.Value = value;
                    }
                    else
                    {
                        db.ValveSettings.Add(new Setting() { Addr = addr, Idx = idx, Time = DateTime.Now, Value = value });
                    }
                    db.SaveChanges();
                }
            }
        }

        //private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    string serialdata = serialPort.ReadLine();
        //    while (serialdata != null)
        //    {
        //        Console.WriteLine(serialdata);
        //        string line = serialdata.Trim();
        //        ProcesSerialData(line);
        //        serialdata = serialPort.ReadLine();
        //    }
        //}

        private void ExecuteCommands(int addr)
        {
            using (IServiceScope scope = scopeFactory.CreateScope())
            {
                HeatAppContext db = scope.ServiceProvider.GetRequiredService<HeatAppContext>();
                List<Command> res;
                res = db.Queue.Where(e => e.Addr == addr).OrderBy(o => o.Time).Take(25).ToList();
                int weight = 0;
                int bank = 0;
                int send = 0;
                StringBuilder query = new StringBuilder();
                foreach (var rec in res)
                {
                    int cw = GetWeight(rec.Data.Substring(0, 1));
                    weight += cw;
                    if (weight > 10)
                    {
                        if (++bank >= 7)
                        {
                            break;
                        }
                        weight = cw;
                    }
                    query.AppendLine("(" + addr.ToString("x02") + "-" + bank.ToString("x") + ")" + rec.Data);
                    send++;
                    rec.Sent = send;
                }
                string q = query.ToString();
                if (q != "")
                {
                    serialPort.WriteLine(q);
                }
                db.SaveChanges();
            }
        }

        private void SaveState(int addr, string message)
        {
            using (IServiceScope scope = scopeFactory.CreateScope())
            {
                HeatAppContext db = scope.ServiceProvider.GetRequiredService<HeatAppContext>();
                List<string> items = message.Split(' ').ToList();
                ValveLog vl = new ValveLog();
                vl.Addr = addr;
                for (int i = 1; i < items.Count; i++)
                {
                    string item = items[i];
                    switch (item.Substring(0, 1))
                    {
                        case "A":
                            vl.Auto = true;
                            break;
                        case "-":
                            vl.Auto = false;
                            break;
                        case "M":
                            vl.Auto = false;
                            break;
                        case "V":
                            vl.Turn = int.Parse(item.Substring(1));
                            break;
                        case "I":
                            vl.Actual = decimal.Parse(item.Substring(1)) / 100;
                            break;
                        case "S":
                            vl.Wanted = decimal.Parse(item.Substring(1)) / 100;
                            break;
                        case "B":
                            vl.Battery = decimal.Parse(item.Substring(1)) / 1000;
                            break;
                        case "E":
                            vl.Error = int.Parse(item.Substring(1), System.Globalization.NumberStyles.HexNumber);
                            break;
                        case "W":
                            vl.Window = true;
                            break;
                        case "L":
                            vl.Locked = true;
                            break;
                        case "X":
                            //$st['force'] = 1;
                            break;
                    }
                }
                DateTime now = DateTime.Now;
                vl.Time = now;
                db.ValveLog.Add(vl);
                db.SaveChanges();
            }
        }

        private static int GetWeight(string item)
        {
            switch (item)
            {
                case "D":
                    return 10;
                case "S":
                    return 4;
                case "W":
                    return 4;
                case "G":
                    return 2;
                case "R":
                    return 2;
                case "T":
                    return 2;
                default:
                    return 10;
            }
        }
    }
}
