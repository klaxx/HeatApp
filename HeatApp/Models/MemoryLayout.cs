using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HeatApp.Models
{
    public static class MemoryLayout
    {
        public static Dictionary<string, MemoryRecord> GetLayout()
        {
            string elj = File.ReadAllText(@"eprom_layout.json");
            return JsonConvert.DeserializeObject<List<MemoryRecord>>(elj).ToDictionary(r => r.name, r => r);
        }
    }

    public class MemoryRecord
    {
        public int idx { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int visible { get; set; }
        public string category { get; set; }
    }
}
