using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Entities;
using CitySimulation.Tools;

namespace CitySimulation.Generation
{
    public abstract class Area
    {
        public string Name { get; set; }
        public int AreaDepth { get; set; }

        public abstract List<Facility> Generate( ref Point startPos);

        public virtual void SetWorkers(IEnumerable<Person> persons)
        {

        }
        public virtual void Clear()
        {
        }
    }
}
