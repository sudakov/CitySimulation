using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Entity
{
    public class Station : Facility
    {
        public LinkedList<Bus> Buses = new LinkedList<Bus>();
        public Station(string name) : base(name)
        {
        }
    }
}
