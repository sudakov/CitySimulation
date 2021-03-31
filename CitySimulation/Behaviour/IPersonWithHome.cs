using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Entity;

namespace CitySimulation.Behaviour
{
    public interface IPersonWithHome
    {
        Facility Home { get; set; }
    }
}
