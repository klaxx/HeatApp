using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeatApp.Models
{
    public class StateMessage
    {
        [JsonProperty("auto")]
        public bool Auto { get; set; }
        [JsonProperty("window")]
        public bool Window { get; set; }
        [JsonProperty("lock")]
        public bool Locked {get;set;}
        [JsonProperty("temp")]
        public decimal Temp { get; set; }
        [JsonProperty("bat")]
        public decimal Battery { get; set; }
        [JsonProperty("temp_wtd")]
        public decimal Wanted { get; set; }
        [JsonProperty("valve_wtd")]
        public int Valve { get; set; }
        [JsonProperty("error")]
        public int Error { get; set; }
        [JsonProperty("last_seen")]
        public int LastContact { get; set; }
    }
}
