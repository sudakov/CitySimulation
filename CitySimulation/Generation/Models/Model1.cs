using System;
using CitySimulation.Entity;
using CitySimulation.Tools;
using System.Collections.Generic;
using System.Linq;

namespace CitySimulation.Generation.Models
{
    public class Model1
    {
        public int DistanceBetweenStations { get; set; }

        public ServicesConfig Services { get; set; }

        public (int, int)[] BusesSpeedAndCapacities { get; set; }

        public Area[] Areas { get; set; }
        public int AreaSpace { get; set; }

        public int OnFootDistance { get; set; }

        private City _city;
        public Dictionary<ResidentialArea, int[]> GetServicesPerArea(int[] servicesCount)
        {
            int[] SumFull(int[] x1, int[] x2, int mul)
            {
                int[] r = new int[x1.Length];
                for (int i = 0; i < x1.Length; i++)
                {
                    r[i] = x1[i] + mul * x2[i];
                }

                return r;
            }

            int[] Sum(int[] x1, int[] x2)
            {
                return SumFull(x1, x2, 1);
            }

            Dictionary<ResidentialArea, int[]> res = new Dictionary<ResidentialArea, int[]>();

            var residentialAreas = Areas.OfType<ResidentialArea>().ToList();
            int sum = residentialAreas.Sum(x => x.GetHousesCount().Sum());
            for (int i = 0; i < residentialAreas.Count-1; i++)
            {
                int count = residentialAreas[i].GetHousesCount().Sum();
                int[] servicesForArea = servicesCount.Select(x => x * count / sum).ToArray();

                res.Add(residentialAreas[i], servicesForArea);
            }

            int[] sum2 = res.Values.Aggregate(Sum);

            res.Add(residentialAreas[residentialAreas.Count - 1], SumFull(servicesCount, sum2, -1));

            return res;
        }


        public City Generate(Dictionary<string, int> familiesPerLivingArea)
        {
            foreach (ResidentialArea residentialArea in Areas.OfType<ResidentialArea>())
            {
                residentialArea.FamiliesCount = familiesPerLivingArea[residentialArea.Name];
            }

            _city = new City();
            int[] servicesCount = Services.ServicesCount();
            var servicesPerArea = GetServicesPerArea(servicesCount);


            Point basePos = new Point(20, 20);
            Point currentPos = new Point(basePos.X, basePos.Y + 200);
            foreach (Area area in Areas)
            {
                List<Facility> facilities;
                if (area is ResidentialArea residentialArea)
                {
                    facilities = residentialArea.GenerateWithServices(
                        ref currentPos, 
                        servicesPerArea[residentialArea],
                        Services.ServicesGenerator
                        );
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
           

            foreach (var area in Areas.OfType<ResidentialArea>())
            {
                area.SetWorkForUnemployed(_city.Persons);
            }

            foreach (var area in Areas.OfType<IndustrialArea>())
            {
                area.SetWorkForUnemployed(_city.Persons);
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
