using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace HeatApp.Models
{
    public class TimeTable
    {
        public TimeTable()
        {
            Timers = new HashSet<Timer>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [MaxLength(100)]
        [Display(Name = "Název")]
        public string Caption { get; set; }
        [MaxLength(10)]
        [Display(Name = "Typ")]
        public string Type { get; set; }
        [Display(Name = "Udržovací")]
        public int NoFrostTemp { get; set; }
        [Display(Name = "Spánek")]
        public int SleepTemp { get; set; }
        [Display(Name = "Ekologická")]
        public int EcoTemp { get; set; }
        [Display(Name = "Komfortní")]
        public int WarmTemp { get; set; }
        public ICollection<Timer> Timers { get; set; }

        [NotMapped]
        public string TimersSerialized { get; set; }
    };
}

