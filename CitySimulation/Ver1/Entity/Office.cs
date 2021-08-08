using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Entities;
using CitySimulation.Tools;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Entity
{
    public class Office : Facility, IWorkplace
    {
        public Office(string name) : base(name)
        {
        }

        public Range WorkTime { get; set; }
        public int WorkersCount { get; set; }
    }
}
