using CitySimulation.Entity;
using CitySimulation.Tools;
using System.Collections.Generic;
using System.Linq;

namespace CitySimulation.Generation.Models
{
    public class Model1
    {
        public struct ServicesConfig
        {
            public int ServiceWorkersCount { get; set; }
            public int MaxWorkersPerService { get; set; }
            public ServicesGenerator ServicesGenerator { get; set; }

            public int[] ServicesCount()
            {
                int[] count = new int[MaxWorkersPerService];
                int sum = (1 + MaxWorkersPerService) * MaxWorkersPerService / 2;

                for (int i = 1; i < MaxWorkersPerService; i++)
                {
                    count[i - 1] = (MaxWorkersPerService - i + 1) * ServiceWorkersCount / sum;
                }
                count[MaxWorkersPerService - 1] = ServiceWorkersCount - count.Sum();
                return count;
            }
        }
        public int Length { get; set; }
        public int DistanceBetweenStations { get; set; }

        public ServicesConfig Services { get; set; }

        public (int, int)[] BusesSpeedAndCapacities { get; set; }

        public Area[] Areas { get; set; }

        public int OnFootDistance { get; set; }


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
            int sum = residentialAreas.Sum(x => x.GetHousesCount());
            for (int i = 0; i < residentialAreas.Count-1; i++)
            {
                int count = residentialAreas[i].GetHousesCount();
                int[] servicesForArea = servicesCount.Select(x => x * count / sum).ToArray();

                res.Add(residentialAreas[i], servicesForArea);
            }

            int[] sum2 = res.Values.Aggregate(Sum);

            res.Add(residentialAreas[residentialAreas.Count - 1], SumFull(servicesCount, sum2, -1));

            return res;
        }


        public City Generate()
        {
            City city = new City();
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
                city.Facilities.AddRange(facilities);
            }

            foreach (var area in Areas.OfType<ResidentialArea>())
            {
                List<Person> generatedPeople = area.GeneratePeople();
                city.Persons.AddRange(generatedPeople);
            }


            foreach (var area in Areas.OfType<ResidentialArea>())
            {
                area.SetWorkForUnemployed(city.Persons);
            }

            foreach (var area in Areas.OfType<IndustrialArea>())
            {
                area.SetWorkForUnemployed(city.Persons);
            }

            int stationsCount = Length / DistanceBetweenStations;
            int startPos = (Length - DistanceBetweenStations * (stationsCount - 1)) / 2;

            List<Station> stations = new List<Station>();

            for (int i = 0; i < stationsCount; i++)
            {
                stations.Add(new Station("S_" + i)
                {
                    Coords = new Point(basePos.X + startPos + i * DistanceBetweenStations, basePos.Y)
                });
            }

            city.Facilities.AddRange(stations);

            for (int i = 0; i < stations.Count; i++)
            {
                for (int j = i + 1; j < stations.Count; j++)
                {
                    city.Facilities.Link(stations[i], stations[j]);
                }
            }

            var busQueueList = stations.Concat(Enumerable.Reverse(stations).Skip(1).Take(stations.Count - 2)).ToList();


            if (BusesSpeedAndCapacities?.Length > 0)
            {
                float k = (2 * stations.Count - 2) / (float)BusesSpeedAndCapacities.Length;

                for (int i = 0; i < BusesSpeedAndCapacities.Length; i++)
                {
                    city.Facilities.Add(new Bus("B_" + i, busQueueList) { Speed = BusesSpeedAndCapacities[i].Item1, Capacity = BusesSpeedAndCapacities[i].Item2 }.SkipStations((int)(i * k)));
                }
            }


            foreach (Facility facility in city.Facilities.Values)
            {
                if (!(facility is Station || facility is Bus))
                {
                    Station closest = stations.MinBy(x => Point.Distance(x.Coords, facility.Coords));
                    city.Facilities.Link(closest, facility);
                }
            }

            for (int i = 0; i < city.Facilities.Count; i++)
            {
                for (int j = i + 1; j < city.Facilities.Count; j++)
                {
                    if (!(city.Facilities[i] is Bus) && !(city.Facilities[j] is Bus))
                    {
                        if (Point.Distance(city.Facilities[i].Coords, city.Facilities[j].Coords) < OnFootDistance)
                        {
                            city.Facilities.Link(city.Facilities[i], city.Facilities[j]);
                        }
                    }
                }
            }

            foreach (Area area in Areas)
            {
                area.Clear();
            }

            return city;
        }
    }
}
