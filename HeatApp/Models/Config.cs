using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HeatApp.Models
{
    public class Config
    {
        [Key]
        public string ConfigID { get; set; }
        public string ConfigValue { get; set; }
    }
}
