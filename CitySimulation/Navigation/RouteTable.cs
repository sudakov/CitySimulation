using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Entity;

namespace CitySimulation.Navigation
{
    public class RouteTable : Dictionary<(Facility, Facility), PathSegment>
    {

    }
}
