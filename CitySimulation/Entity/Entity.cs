using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Tools;

namespace CitySimulation.Entity
{
    public abstract class Entity
    {
        public readonly string Name;
        public Point Coords;

        protected Entity()
        {
            Name = Guid.NewGuid().ToString();
        }

        protected Entity(string name)
        {
            Name = name;
        }

        public virtual void PreProcess()
        {

        }

        public virtual void Process()
        {

        }

        public virtual void PostProcess()
        {

        }

        public override string ToString()
        {
            return Name;
        }
    }
}
