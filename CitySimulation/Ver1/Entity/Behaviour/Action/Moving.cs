using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Entities;
using CitySimulation.Entity;
using CitySimulation.Navigation;

namespace CitySimulation.Behaviour.Action
{
    public class Moving : EntityAction
    {
        public Link Link;
        public Facility Destination;
        public int DistanceCovered;

        public Moving(Link link, Facility destination)
        {
            Link = link;
            Destination = destination;
        }


        public override string ToString()
        {
            return Link.ToString() + $": {DistanceCovered} ({Destination})";
        }
    }
}
