using System;
using CitySimulation.Entity;
using CitySimulation.Tools;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CitySimulation.Behaviour;

namespace CitySimulation.Generation.Models
{
    public class Model1
    {
        public AgesConfig AgesConfig { get; set; }
        public int DistanceBetweenStations { get; set; }

        public ServicesConfig ServicesConfig { get; set; }

        public (int, int)[] BusesSpeedAndCapacities { get; set; }

        public Area[] Areas { get; set; }
        public int AreaSpace { get; set; }

        public int OnFootDistance { get; set; }

        private City _city;

        public City Generate(Dictionary<string, int> familiesPerLivingArea)
        {
            foreach (ResidentialArea residentialArea in Areas.OfType<ResidentialArea>())
            {
                residentialArea.FamiliesCount = familiesPerLivingArea[residentialArea.Name];
            }

            _city = new City();


            Point basePos = new Point(20, 20);
            Point currentPos = new Point(basePos.X, basePos.Y + 200);
            foreach (Area area in Areas)
            {
                List<Facility> facilities;
                if (area is ResidentialArea residentialArea)
                {
                    facilities = residentialArea.GenerateWithServices(ref currentPos, ServicesConfig);
                }
                else
                {
                    facilities = area.Generate(ref currentPos);
                }

                currentPos.X += AreaSpace;
                _city.Facilities.AddRange(facilities);
            }

            int length = currentPos.X - basePos.X;
            int stationsCount = length / DistanceBetweenStations;
            int startPos = (length - DistanceBetweenStations * (stationsCount - 1)) / 2;

            List<Station> stations = new List<Station>();

            for (int i = 0; i < stationsCount; i++)
            {
                stations.Add(new Station("S_" + i)
                {
                    Coords = new Point(basePos.X + startPos + i * DistanceBetweenStations, basePos.Y)
                });
            }

            _city.Facilities.AddRange(stations);

            for (int i = 0; i < stations.Count; i++)
            {
                for (int j = i + 1; j < stations.Count; j++)
                {
                    _city.Facilities.Link(stations[i], stations[j]);
                }
            }

            var busQueueList = stations.Concat(Enumerable.Reverse(stations).Skip(1).Take(stations.Count - 2)).ToList();


            if (BusesSpeedAndCapacities?.Length > 0)
            {
                float k = (2 * stations.Count - 2) / (float)BusesSpeedAndCapacities.Length;

                for (int i = 0; i < BusesSpeedAndCapacities.Length; i++)
                {
                    _city.Facilities.Add(new Bus("B_" + i, busQueueList) { Speed = BusesSpeedAndCapacities[i].Item1, Capacity = BusesSpeedAndCapacities[i].Item2 }.SkipStations((int)(i * k)));
                }
            }


            foreach (Facility facility in _city.Facilities.Values)
            {
                if (!(facility is Station || facility is Bus))
                {
                    Station closest = stations.MinBy(x => Point.Distance(x.Coords, facility.Coords));
                    _city.Facilities.Link(closest, facility);
                }
            }

            for (int i = 0; i < _city.Facilities.Count; i++)
            {
                for (int j = i + 1; j < _city.Facilities.Count; j++)
                {
                    if (!(_city.Facilities[i] is Bus) && !(_city.Facilities[j] is Bus))
                    {
                        if (Point.Distance(_city.Facilities[i].Coords, _city.Facilities[j].Coords) < OnFootDistance)
                        {
                            _city.Facilities.Link(_city.Facilities[i], _city.Facilities[j]);
                        }
                    }
                }
            }
            
            return _city;
        }

        public void Populate(Dictionary<string, int> familiesPerLivingArea, IEnumerable<Family> families)
        {
            _city.Persons = new List<Person>(families.SelectMany(x=>x.Members));

            {
                List<Family> families_copy = new List<Family>(families);
                foreach (var area in Areas.OfType<ResidentialArea>())
                {
                    area.Populate(families_copy.PopItems(familiesPerLivingArea[area.Name]));
                }
            }
           

            foreach (var area in Areas)
            {
                area.SetWorkers(_city.Persons);
            }

            SetWorkForServices();

            //Задаём кол-во посетителей в месяц административных зданий
            int adultCount = _city.Persons.Count(x => AgesConfig.AdultAge.InRange(x.Age));
            foreach (Facility facility in _city.Facilities.Values)
            {
                if (facility is AdministrativeService service)
                {
                    service.VisitorsPerMonth = (int) (adultCount * (Controller.Random.NextDouble() * (5 - 1) + 1) / 100);
                }
            }
        }

        public void SetWorkForServices()
        {
            var unemployed = _city.Persons.Where(x => x.Behaviour is IPersonWithWork w && w.WorkPlace == null).ToList();

            int toEmploy = (int)(_city.Persons.Count(x=> x.Behaviour is IPersonWithWork) * ServicesConfig.ServiceWorkersRatio);

            List<Service> services = _city.Facilities.Values.OfType<Service>().Shuffle(Controller.Random).ToList();

            Dictionary<Service, int> map = services.ToDictionary(x => x, x => x.WorkersCount - _city.Persons.Count(y=>y.Behaviour is IPersonWithWork behaviour && behaviour.WorkPlace == x));

            services.RemoveAll(x => map[x] <= 0);

            {
                foreach (Service service in services)
                {
                    var local = unemployed.Where(x => Point.Distance(service.Coords, x.Home.Coords) < 1000).Take((int)(map[service] * ServicesConfig.LocalWorkersRatio)).ToList();
                    local.ForEach(x => unemployed.Remove(x));

                    map[service] -= local.Count;
                    toEmploy -= local.Count;
                    local.ForEach(x => (x.Behaviour as IPersonWithWork)?.SetWorkplace(service));
                }
            }

            var stack = new Stack<Person>(unemployed);
            while (stack.Any() && services.Any() && toEmploy > 0)
            {
                for (int i = services.Count - 1; i >= 0; i--)
                {
                    if (stack.Any() && toEmploy > 0)
                    {
                        var behaviour = stack.Pop();
                        (behaviour.Behaviour as IPersonWithWork).SetWorkplace(services[i]);
                        if (--map[services[i]] == 0)
                        {
                            services.RemoveAt(i);
                        }

                        toEmploy--;
                    }
                }
            }

            if (stack.Any() && services.Any() && toEmploy > 0)
            {
                //Рабочие места закончились, так что будем заполнять сверх нормы
                services = _city.Facilities.Values.OfType<Service>().Shuffle(Controller.Random).ToList();
                map = services.Where(x=> x.MaxWorkersCount - x.WorkersCount > 0).ToDictionary(x => x, x => x.MaxWorkersCount - _city.Persons.Count(y => y.Behaviour is IPersonWithWork behaviour && behaviour.WorkPlace == x));
                services.RemoveAll(x => map[x] <= 0);

                while (stack.Any() && services.Any() && toEmploy > 0)
                {
                    for (int i = services.Count - 1; i >= 0; i--)
                    {
                        if (stack.Any() && toEmploy > 0)
                        {
                            var behaviour = stack.Pop();
                            (behaviour.Behaviour as IPersonWithWork).SetWorkplace(services[i]);
                            if (--map[services[i]] == 0)
                            {
                                services.RemoveAt(i);
                            }

                            toEmploy--;
                        }
                    }
                }
            }

            if (toEmploy > 0)
            {
                if (unemployed.Any())
                {
                    Debug.WriteLine("Не всем хватило рабочих мест в сфере сервиса: " + unemployed.Count);
                }
                else
                {
                    Debug.WriteLine("Доля свободных рабочих не соответствует запрашиваемой");
                }
            }
        }

        public void Clear()
        {
            foreach (Area area in Areas)
            {
                area.Clear();
            }

            _city = null;
        }
    }
}
