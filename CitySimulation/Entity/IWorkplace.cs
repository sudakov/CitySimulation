using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Tools;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Entity
{
    interface IWorkplace
    {
        Range WorkTime { get; }
        int WorkersCount { get; set; }

    }
}
