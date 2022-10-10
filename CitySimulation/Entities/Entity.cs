using System;
using CitySimulation.Control;
using CitySimulation.Tools;

namespace CitySimulation.Entities
{
    public abstract class EntityBase
    {
        public int Id { get; set; }
        public string Name;
        public string NameMember => Name;

        public Point Coords;
        public Context Context;
        protected EntityBase()
        {
            Name = Guid.NewGuid().ToString();
        }

        protected EntityBase(string name)
        {
            Name = name;
        }

        public virtual void Setup()
        {
        }

        public virtual void PreRun()
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

        public virtual Point? CalcCoords()
        {
            return Coords;
        }
    }
}
