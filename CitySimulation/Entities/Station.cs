using System.Collections.Generic;

namespace CitySimulation.Entities
{
    public class Station : Facility
    {
        public LinkedList<Bus> Buses = new LinkedList<Bus>();
        public Station(string name) : base(name)
        {
            Type = "Station";
        }
    }
}
