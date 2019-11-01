using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeatApp.Models
{
    public class HeatAppContext : IdentityDbContext
    {
        private static readonly Dictionary<string, MemoryRecord> memoryLayout = MemoryLayout.GetLayout();

        public HeatAppContext(DbContextOptions<HeatAppContext> options)
            : base(options)
        {
        }

        public DbSet<Valve> Valves { get; set; }
        public DbSet<TimeTable> Timetables { get; set; }
        public DbSet<Timer> Timers { get; set; }
        public DbSet<Command> Queue { get; set; }
        public DbSet<Setting> ValveSettings { get; set; }
        public DbSet<ValveLog> ValveLog { get; set; }
        public DbSet<Config> Configs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TimeTable>(entity =>
            {
                //entity.HasMany(t => t.Timers).WithOne();
            });

            modelBuilder.Entity<Timer>()
                .HasKey(c => new { c.Idx, c.TimeTableId, c.Addr });

            modelBuilder.Entity<Setting>()
                .HasKey(c => new { c.Idx, c.Addr });

            modelBuilder.Entity<Config>().HasData(new Config[] {
                new Config{ConfigID="BoilerEnabled",ConfigValue="off"}
            });
        }

    }
}
