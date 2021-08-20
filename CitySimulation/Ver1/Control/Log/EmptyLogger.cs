using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Entities;
using CitySimulation.Tools;
using CitySimulation.Ver1.Entity;

namespace CitySimulation.Control.Log
{
    public class EmptyLogger : Logger
    {
        public override void LogPersonInFacilityTime(LogCityTime start, LogCityTime end, Facility facility, Person person)
        {
            throw new NotImplementedException();
        }

        public override int? Start()
        {
            return null;
        }

        public override void Stop()
        {
        }

        public override void LogVisit(Service service)
        {
            throw new NotImplementedException();
        }
    }
}
