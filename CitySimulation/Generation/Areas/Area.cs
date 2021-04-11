using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Entity;
using CitySimulation.Tools;

namespace CitySimulation.Generation
{
    public abstract class Area
    {
        public string Name { get; set; }

        public abstract List<Facility> Generate( ref Point startPos);

        public virtual void SetWorkForUnemployed(IEnumerable<Person> persons)
        {

        }
        public virtual void Clear()
        {
        }
    }
}
