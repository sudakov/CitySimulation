using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Tools;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Entity
{
    public class Service : Facility, IWorkplace
    {
        private static int _nextId;
        public static int GetId()
        {
            return _nextId++;
        }

        public Service(string name) : base(name)
        {
        }

        public Range WorkTime { get; set; }
        public int WorkersCount { get; set; }
        public int MaxWorkersCount { get; set; }
    }
}
