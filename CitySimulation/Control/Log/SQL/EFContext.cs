using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Control.Log.DbModel;
using Microsoft.EntityFrameworkCore;

namespace CitySimulation.Control.Log.SQL
{
    public class EFContext : DbContext
    {
        public DbSet<Session> Sessions { get; set; }
        public DbSet<PersonInFacilityTime> PersonInFacilityTimes { get; set; }

        public EFContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // optionsBuilder.UseSqlServer($"Server=(localdb)\\mssqllocaldb;AttachDbFilename={AppContext.BaseDirectory}DATABASE.MDF;Trusted_Connection=True;");
            optionsBuilder.UseSqlServer($"Server=(localdb)\\mssqllocaldb;DATABASE=CitySimulation;Trusted_Connection=True;");
        }
    }
}
