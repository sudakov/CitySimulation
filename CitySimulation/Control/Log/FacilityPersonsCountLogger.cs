using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CitySimulation.Entity;
using CitySimulation.Tools;

namespace CitySimulation.Control.Log
{
    public class FacilityPersonsCountLogger : Logger
    {
        private Dictionary<Facility, LinkedList<(int, int)>> _countData = new Dictionary<Facility, LinkedList<(int, int)>>();

        public override void LogPersonInFacilityTime(LogCityTime start, LogCityTime end, Facility facility, Person person)
        {
        }

        public override int? Start()
        {
            _countData.Clear();
            foreach (var facility in Controller.Instance.City.Facilities.Values)
            {
                _countData.Add(facility, new LinkedList<(int, int)>(new[] { (0, 0) }));
            }

            return null;
        }

        public override void Stop()
        {
        }

        public override void PostProcess()
        {
            foreach (var pair in _countData)
            {
                int current = pair.Key.PersonsCount;
                (int, int) prev = pair.Value.Last.Value;
                if (prev.Item2 != current)
                {
                    int time = Controller.CurrentTime.TotalMinutes;
                    if (time != 0 && (time - Controller.Instance.DeltaTime) != prev.Item1)
                    {
                        pair.Value.AddLast((time - Controller.Instance.DeltaTime, prev.Item2));

                    }
                    pair.Value.AddLast((time, current));
                }
            }
        }

        public LinkedList<(int, int)> GetDataForFacility(string facilityName)
        {
            Facility facility = _countData.Keys.FirstOrDefault(x=>x.Name == facilityName);

            return _countData[facility];
        }

        public Dictionary<string, LinkedList<(int, int)>> GetData()
        {
            return _countData.ToDictionary(x => x.Key.Name, x => x.Value);
        }
    }
}
