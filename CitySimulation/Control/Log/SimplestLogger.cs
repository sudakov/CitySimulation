using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CitySimulation.Entity;
using CitySimulation.Tools;

namespace CitySimulation.Control.Log
{
    public class SimplestLogger : Logger
    {
        // private ConcurrentBag<(uint, uint, Facility, Person)> log = new ConcurrentBag<(uint, uint, Facility, Person)>();

        private Dictionary<Facility, LinkedList<(int, int)>> _countData = new Dictionary<Facility, LinkedList<(int, int)>>();

        public override void LogPersonInFacilityTime(LogCityTime start, LogCityTime end, Facility facility, Person person)
        {
            // log.Add(((uint)start.Day*24*60 + (uint)start.Minutes, (uint)end.Day*24*60+ (uint)end.Minutes, facility, person));
        }

        public override int? Start()
        {
            foreach (var facility in Controller.Instance.City.Facilities.Values)
            {
                _countData.Add(facility, new LinkedList<(int, int)>(new []{(0,0)}));
            }

            return null;
        }

        public override void Stop()
        {
            
        }

        public override void LogVisit(Service service)
        {
            
        }

        public override void PostProcess()
        {
            foreach (var pair in _countData)
            {
                int current = pair.Key.PersonsCount;
                if (pair.Value.Last.Value.Item2 != current)
                {
                    pair.Value.AddLast((Controller.Context.CurrentTime.TotalMinutes, current));
                }
            }

        }
    }
}
