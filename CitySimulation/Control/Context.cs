using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Navigation;

namespace CitySimulation.Control
{
    public class Context
    {
        public Logger Logger;
        public RouteTable Routes;
        public Random Random;
        public CityTime CurrentTime = new CityTime();
    }
}
