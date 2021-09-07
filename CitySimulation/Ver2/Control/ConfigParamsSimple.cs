using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Control;
using CitySimulation.Generation.Model2;
using Newtonsoft.Json;

namespace CitySimulation.Ver2.Control
{
    public class ConfigParamsSimple : ConfigParams
    {
        public RandomWeibullParams IncubationToSpreadDelay { get; set; }
        public RandomWeibullParams SpreadToImmuneDelay { get; set; }
        public double DeathProbability { get; set; }
    }
}
