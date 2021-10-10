using System.Collections.Generic;

namespace CitySimulation.Entities
{
    public class Station : Facility
    {
        public LinkedList<Transport> Buses = new LinkedList<Transport>();
        public Station(string name) : base(name)
        {
            Type = "Station";
        }
    }
}
