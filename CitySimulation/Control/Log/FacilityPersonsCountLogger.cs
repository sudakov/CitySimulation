using System;
using System.Collections.Concurrent;
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
        private Dictionary<Service, ConcurrentDictionary<int, int>> _visitorsData = new Dictionary<Service, ConcurrentDictionary<int, int>>();

        public override void LogPersonInFacilityTime(LogCityTime start, LogCityTime end, Facility facility, Person person)
        {
        }

        public override int? Start()
        {
            _countData.Clear();
            foreach (var facility in Controller.Instance.City.Facilities.Values)
            {
                _countData.Add(facility, new LinkedList<(int, int)>(new[] { (0, 0) }));
                if (facility is Service service)
                {
                    _visitorsData.Add(service, new ConcurrentDictionary<int, int>());
                }
            }

            return null;
        }

        public override void Stop()
        {
        }

        private int visitStepMinutes = 60;
        public override void LogVisit(Service service)
        {
            int totalMinutes = Controller.CurrentTime.TotalMinutes;
            _visitorsData[service].AddOrUpdate(totalMinutes - totalMinutes % visitStepMinutes, tuple => 1, (tuple, old) => old + 1);
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

        public Dictionary<string, LinkedList<(int, int)>> GetVisitorsData()
        {
            Dictionary<string, LinkedList<(int, int)>> res = new Dictionary<string, LinkedList<(int, int)>>();

            foreach (var service in _visitorsData.Keys)
            {
                var list = new List<(int,int)>(_visitorsData[service].Select(x => (x.Key, x.Value)).OrderBy(x=>x.Key));
                LinkedList<(int,int)> res_list = new LinkedList<(int, int)>();
                int accum = 0;
                int lastday = -1;
                for (var i = 0; i < list.Count; i++)
                {
                    int day = list[i].Item1 / (60 * 24);
                    var d = new LogCityTime(day, list[i].Item1 % (60 * 24));

                    if (day != lastday)
                    {
                        if(i != 0)
                            res_list.AddLast((list[i-1].Item1 + visitStepMinutes - 1, 0));
                        accum = 0;
                        lastday = day;
                    }

                    accum += list[i].Item2;
                    res_list.AddLast((list[i].Item1, accum));
                }
                res.Add(service.Name, res_list);
            }

            return res;
        }
    }
}
