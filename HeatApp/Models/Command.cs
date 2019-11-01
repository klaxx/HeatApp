using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace HeatApp.Models
{
    public class Command
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int Addr { get; set; }
        public int Sent { get; set; }
        public DateTime Time { get; set; }
        [MaxLength(20)]
        public string Data { get; set; }
    }
}
