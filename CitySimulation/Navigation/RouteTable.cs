using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using CitySimulation.Entity;

namespace CitySimulation.Navigation
{
    public class RouteTable : Dictionary<(Facility, Facility), PathSegment>
    {
        public double LongestRoute;


        public void Setup()
        {
            LongestRoute = Values.Any() ? Values.Max(x=>x.TotalLength) : 0;
        }
    }
}
