using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Entity;

namespace CitySimulation.Behaviour
{
    public interface IPersonWithWork
    {
        Facility WorkPlace { get; }

        void SetWorkplace(Facility workplace);
    }
}
