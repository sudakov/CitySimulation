using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Entity;
using CitySimulation.Tools;

namespace CitySimulation.Generation
{
    public class ServicesGenerator
    {
        public TimeRange WorkTime { get; set; } = new TimeRange(8*60, 17*60);
        public int WorkTimeTolerance { get; set; }

        public Service Generate(string name, int workersCount)
        {
            int rand = Controller.Random.Next(-WorkTimeTolerance, WorkTimeTolerance+1);
            return new Service(name)
            {
                WorkersCount = workersCount,
                WorkTime = new TimeRange(WorkTime.Start + rand*60, WorkTime.End + rand * 60)
            };
        }
    }
}
