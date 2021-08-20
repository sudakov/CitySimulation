using CitySimulation.Entities;
using CitySimulation.Tools;
using CitySimulation.Ver1.Entity;

namespace CitySimulation.Control
{
    public abstract class Logger : Module
    {
        public int SessionId { get; protected set; } = -1;

        public abstract void LogPersonInFacilityTime(LogCityTime start, LogCityTime end, Facility facility, Person person);

        public abstract int? Start();

        public abstract void Stop();

        public abstract void LogVisit(Service service);
    }
}
