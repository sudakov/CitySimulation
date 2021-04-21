using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Tools;

namespace CitySimulation.Entity
{
    public abstract class Entity
    {
        public readonly string Name;
        public string NameMember => Name;

        public Point Coords;
        public Controller Controller;
        protected Entity()
        {
            Name = Guid.NewGuid().ToString();
        }

        protected Entity(string name)
        {
            Name = name;
        }

        public virtual void Setup(Controller controller)
        {
            Controller = controller;
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
