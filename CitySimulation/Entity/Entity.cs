using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Control;
using CitySimulation.Tools;

namespace CitySimulation.Entity
{
    public abstract class Entity
    {
        public int Id { get; set; }
        public string Name;
        public string NameMember => Name;

        public Point Coords;
        public Context Context;
        protected Entity()
        {
            Name = Guid.NewGuid().ToString();
        }

        protected Entity(string name)
        {
            Name = name;
        }

        public virtual void Setup()
        {
        }

        public void PreRun()
        {

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
