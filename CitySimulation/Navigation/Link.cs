using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Entity;

namespace CitySimulation.Navigation
{
    public class Link
    {
        public Facility From;
        public Facility To;
        public double Length;

        public Link(Facility from, Facility to, double length)
        {
            From = from;
            To = to;
            Length = length;
        }

        public override string ToString()
        {
            return " -> " + To.Name;
        }
    }
}
