using System.Collections.Generic;
using CitySimulation.Entities;

namespace CitySimulation.Ver1.Entity
{
    public class Station : Facility
    {
        public LinkedList<Bus> Buses = new LinkedList<Bus>();
        public Station(string name) : base(name)
        {
        }
    }
}
