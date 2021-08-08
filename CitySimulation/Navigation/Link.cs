using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Entities;
using CitySimulation.Entity;

namespace CitySimulation.Navigation
{
    public class Link
    {
        public Facility From;
        public Facility To;
        public double Length;
        public double Time;

        public Link(Facility from, Facility to, double length)
        {
            From = from;
            To = to;
            Length = length;
            Time = length;
        }

        public Link(Facility from, Facility to, double length, double time)
        {
            From = from;
            To = to;
            Length = length;
            Time = time;
        }

        public override string ToString()
        {
            return " -> " + To.Name;
        }
    }
}
