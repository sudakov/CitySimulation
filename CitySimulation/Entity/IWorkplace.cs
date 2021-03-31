using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Tools;

namespace CitySimulation.Entity
{
    interface IWorkplace
    {
        TimeRange WorkTime { get; }
        int WorkersCount { get; set; }

    }
}
