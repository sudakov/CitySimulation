using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CitySimulation.Entities;
using CitySimulation.Generation.Model2;
using CitySimulation.Health;
using CitySimulation.Tools;
using CitySimulation.Ver1.Entity;
using CitySimulation.Ver2.Control;
using CitySimulation.Ver2.Entity;
using CitySimulation.Ver2.Entity.Behaviour;
using Newtonsoft.Json;
using Station = CitySimulation.Entities.Station;

namespace CitySimulation.Ver2.Generation
{
    public class ModelSimple
    {
        public string FileName;
        public bool UseTransport { get; set; }

        public City Generate(Random random)
        {
            var text = File.ReadAllText(FileName);
            var data = JsonConvert.DeserializeObject<JsonModel>(text);


            var locationGroups = data.LinkLocPeopleTypes.GroupBy(x => x.PeopleType)
                .ToDictionary(x => x.Key, x => x.Select(y=>(y, new List<FacilityConfigurable>())).ToList());

            data.LinkLocPeopleTypes.ForEach(x=>x.Income ??= new List<Income>(0));

            List<Facility> facilities = new List<Facility>();

            List<Person> persons = new List<Person>();

            int k = 0;
            int PersonIdOffset = 100000;

            foreach (KeyValuePair<string, LocationType> locationType in data.LocationTypes)
            {
                for (int i = 0; i < locationType.Value.Num; i++)
                {
                    Facility facility;
                    if (data.TransportStationLinks.Any(x => x.StationType == locationType.Key))
                    {
                        facility = new Station(locationType.Key + "_" + i);
                    }
                    else
                    {
                        facility = new FacilityConfigurable(locationType.Key + "_" + i);
                    }

                    facility.Type = locationType.Key;
                    facility.InfectionProbability = locationType.Value.InfectionProbability;
                    facility.Behaviour = new ConfigurableFacilityBehaviour();


                    facilities.Add(facility);

                    int peopleCount = locationType.Value.PeopleMean == 0 ? 0 : random.RollPuassonInt(locationType.Value.PeopleMean);

                    var personTypeFractions = data.PeopleTypes
                        .ToDictionary(x => x.Key,
                            x => (x.Value, data.LinkLocPeopleTypes.FirstOrDefault(y => y.LocationType == locationType.Key && x.Key == y.PeopleType)));

                    double sumWeight = personTypeFractions.Where(x=>x.Value.Item2 != null).Sum(x => x.Value.Value.Fraction);

                    foreach (var personTypeFraction in personTypeFractions.Where(x=>x.Value.Item2 != null))
                    {
                        int count = (int)Math.Round(peopleCount * personTypeFraction.Value.Value.Fraction / sumWeight);
                        for (int j = 0; j < count; j++)
                        {
                            ConfigurableBehaviour behaviour;
                            if (UseTransport)
                            {
                                behaviour = new ConfigurableBehaviourWithTransport()
                                {
                                    Type = personTypeFraction.Key,
                                    AvailableLocations = locationGroups.GetValueOrDefault(personTypeFraction.Key)
                                };
                            }
                            else
                            {
                                behaviour = new ConfigurableBehaviour()
                                {
                                    Type = personTypeFraction.Key,
                                    AvailableLocations = locationGroups.GetValueOrDefault(personTypeFraction.Key)
                                };
                            }
                            

                            var person = new Person(personTypeFraction.Key + "_" + k)
                            {
                                Behaviour = behaviour,
                                Id = k + PersonIdOffset
                            };

                            k++;

                            person.HealthData = new HealthDataSimple(person);

                            persons.Add(person);

                            // person.SetLocation(facility);


                            if (personTypeFraction.Value.Item2.Ispermanent != 0)
                            {
                                behaviour.PersistentFacilities.Add(personTypeFraction.Key, (FacilityConfigurable)facility);
                            }
                        }
                    }
                }
            }

            var param = new ConfigParamsSimple()
            {
                DeathProbability = data.DeathProbability,
                IncubationToSpreadDelay = data.IncubationToSpreadDelay,
                SpreadToImmuneDelay = data.SpreadToImmuneDelay,
            };

            CityTime time = new CityTime();

            foreach (var (key, peopleType) in data.PeopleTypes)
            {
                persons.Where(x => ((ConfigurableBehaviour)x.Behaviour).Type == key).Shuffle(random)
                    .Take(peopleType.StartInfected).ToList().ForEach(x => (x.HealthData as HealthDataSimple).TryInfect(param, time, random));
            }


            int size = 80;

            facilities = facilities.Shuffle(random).ToList();

            Point locationsDistance = GetLocationsDistance(facilities.Count, data.Geozone);

            {
                Point point = new Point(locationsDistance.X / 2, locationsDistance.Y / 2);

                List<Point> points = new List<Point>();
                foreach (var facility in facilities)
                {
                    points.Add(new Point(point));

                    point.X += locationsDistance.X;

                    if (point.X > data.Geozone.X)
                    {
                        point.Y += locationsDistance.Y;
                        point.X = locationsDistance.X / 2;
                    }

                }

                points = points.Shuffle(random).ToList();

                List<Point> stationPoints = SelectUniformPoints(points, (int)Point.Distance(new Point(0,0), data.Geozone), facilities.OfType<Station>().Count());

                stationPoints.ForEach(x => points.Remove(x));

                foreach (var facility in facilities)
                {
                    facility.Size = new Point(size, size);
                    if (facility is Station)
                    {
                        facility.Coords = stationPoints[^1];
                        stationPoints.RemoveAt(stationPoints.Count - 1);
                    }
                    else
                    {
                        facility.Coords = points[^1];
                        points.RemoveAt(points.Count - 1);
                    }
                }
            }

            // foreach (var facility in facilities)
            // {
            //     facility.Size = new Point(size, size);
            //     facility.Coords = new Point(point);
            //
            //     point.X += locationsDistance.X;
            //
            //     if (point.X > data.Geozone.X)
            //     {
            //         point.Y += locationsDistance.Y;
            //         point.X = locationsDistance.X / 2;
            //     }
            //
            // }

            foreach (var pair in locationGroups)
            {
                foreach (var pair2 in pair.Value)
                {
                    pair2.Item2.AddRange(facilities.OfType<FacilityConfigurable>().Where(x => x.Type == pair2.y.LocationType));
                }
            }

            City city = new City()
            {
                Persons = persons
            };

            city.Facilities.AddRange(facilities);

            if (UseTransport)
            {

                //GenerateBuses(data, city);
                List<TransportStationLink> stationLinks = data.TransportStationLinks.ToList();

                Dictionary<string, List<List<Station>>> routes = new Dictionary<string, List<List<Station>>>();

                foreach (var link in stationLinks)
                {

                    List<Station> stations_all = facilities.Where(x => x.Type == link.StationType).OfType<Station>().ToList();

                    List<Station> without_route = new List<Station>(stations_all);

                    for (int i = 0; i < link.RouteCount; i++)
                    {
                        List<Station> stations = new List<Station>(stations_all);
                        int routeLen = random.Next(link.RouteMinStations, link.RouteMaxStations + 1);
                        Station base_station = without_route.Any() ? without_route.GetRandom(random) : stations.GetRandom(random);

                        without_route.Remove(base_station);
                        stations.Remove(base_station);

                        LinkedList<Station> route = new LinkedList<Station>();
                        route.AddFirst(base_station);

                        for (int j = 0; j < routeLen - 1; j++)
                        {
                            Station left = stations.MinBy(x => Point.Distance(route.First.Value.Coords, x.Coords));
                            Station right = stations.MinBy(x => Point.Distance(route.Last.Value.Coords, x.Coords));

                            if (Point.Distance(left.Coords, route.First.Value.Coords) > Point.Distance(right.Coords, route.Last.Value.Coords))
                            {
                                route.AddLast(right);
                                stations.Remove(right);
                                without_route.Remove(right);
                            }
                            else
                            {
                                route.AddFirst(left);
                                stations.Remove(left);
                                without_route.Remove(left);
                            }
                        }


                        var transportList = routes.GetOrSetDefault(link.TransportType, new List<List<Station>>());
                        transportList.Add(route.ToList());
                    }

                    // //add stations without route to some routes
                    // if (without_route.Any())
                    // {
                    //     foreach (var station in without_route)
                    //     {
                    //         foreach (var pair in routes)
                    //         {
                    //             var min1 = pair.Value.MinBy(x => Point.Distance(x.Last().Coords, station.Coords));
                    //             var min2 = pair.Value.MinBy(x => Point.Distance(x.First().Coords, station.Coords));
                    //             if (Point.Distance(min1.Last().Coords, station.Coords) > Point.Distance(min2.First().Coords, station.Coords))
                    //             {
                    //                 min2.Insert(0, station);
                    //             }
                    //             else
                    //             {
                    //                 min1.Add(station);
                    //             }
                    //         }
                    //         // city.Facilities.Remove(station.Name);
                    //     }
                    // }

                }

                int n = 0;

                //Split transport between routes
                foreach (var pair in routes)
                {
                    var transportData = data.Transport[pair.Key];

                    List<double> length_list = pair.Value.Select(x=>(double)x.Count).ToList();
                    double total_length = length_list.Sum();

                    length_list = length_list.Select(x => transportData.Count * x / total_length).ToList();

                    List<int> length_list_int = length_list.Select(x => (int) x).ToList();

                    while (transportData.Count > length_list_int.Sum())
                    {
                        int index = length_list.GetMaxIndex(x=> x - (int)x);
                        length_list_int[index]++;
                        length_list[index] = length_list_int[index];
                    }

                    for (int i = 0; i < length_list_int.Count; i++)
                    {
                        var route = pair.Value[i];
                        for (int j = 0; j < length_list_int[i]; j++)
                        {
                            var route2 = new List<Station>(route);
                            route2.Reverse();
                            route2 = route.Take(route.Count -1).Skip(1).Concat(route2).ToList();



                            Transport bus = new Transport("bus_" + n++, route2)
                            {
                                Type = pair.Key,
                                Speed = RandomFunctions.RollNormalInt(random, transportData.SpeedMean, transportData.SpeedStd),
                                Behaviour = new ConfigurableFacilityBehaviour(),
                                InfectionProbability = transportData.InfectionProbability,
                                Station = route2.GetRandom(random),
                                Capacity = int.MaxValue,
                            };
                            city.Facilities.Add(bus);
                        }
                    }

                    for (int i = 0; i < length_list_int.Count; i++)
                    {
                        if (length_list_int[i] == 0)
                        {
                            pair.Value.RemoveAt(i);
                        }
                    }
                }

                //links creation
                for (int i = 0; i < city.Facilities.Count; i++)
                {
                    for (int j = i + 1; j < city.Facilities.Count; j++)
                    {
                        var f1 = city.Facilities[i];
                        var f2 = city.Facilities[j];

                        if (!(f1 is Transport) && !(f2 is Transport))
                        {
                            bool haveRoute = false;

                            if (f1 is Station && f1.Type == f2.Type)
                            {
                                //only stations with routes should have shorter move time
                                haveRoute = routes.Values.SelectMany(x => x).Any(x => x.Contains(f1) && x.Contains(f2));
                            }

                            double len = Point.Distance(f1.Coords, f2.Coords);
                            city.Facilities.Link(city.Facilities[i], city.Facilities[j], len, haveRoute ? len / 5 : len);
                        }
                    }
                }
            }

            return city;
        }

        private Point GetLocationsDistance(int count, Point size)
        {
            double k = (double)size.Y / size.X;

            double c = Math.Sqrt(count / k);


            double h = 10;
            double d = 2;

            while (true)
            {
                if ((c + 1) * (h + d) < size.X/* && (h + d) * k < size.Y*/)
                {
                    h += d;
                    d *= 2;
                }
                else
                {
                    d /= 2;
                    if (d < 0.0001)
                    {
                        return new Point((int) h, (int) (h * k));
                    }
                }
            }
        }


        private List<Point> SelectUniformPoints(List<Point> points, int startDistance, int count)
        {

            double distance = startDistance;
            double k = 10;

            while (true)
            {
                List<Point> list = new List<Point>(points);
                List<Point> result = new List<Point>();

                while (list.Any())
                {

                    Point p = list[^1];
                    list.RemoveAll(x => Point.Distance(x, p) < distance);

                    result.Add(p);

                    if (result.Count == count)
                    {
                        return result;
                    }
                }

                while (distance - startDistance / k <= 0)
                {
                    k += 1;
                }

                distance -= startDistance / k;
            }
        }


        // private void GenerateBuses(JsonModel data, City city)
        // {
        //     List<Station> stations = new List<Station>();
        //
        //     foreach ((string key, StationData value) in data.Stations)
        //     {
        //         stations.Add(new Station(key)
        //         {
        //             Coords = value.Position,
        //             Size = (30,30),
        //             Behaviour = new ConfigurableFacilityBehaviour(),
        //             InfectionProbability = data.StationInfectionProbability ?? 0
        //         });
        //     }
        //
        //     city.Facilities.AddRange(stations);
        //     
        //     for (int i = 0; i < stations.Count; i++)
        //     {
        //         for (int j = i + 1; j < stations.Count; j++)
        //         {
        //             city.Facilities.Link(stations[i], stations[j], 0.0000000001);
        //         }
        //     }
        //
        //     foreach ((string key, TransportData value) in data.Buses)
        //     {
        //         var busStations  = value.Stations.Select(x=>city.Facilities[x]).Cast<Station>().ToList();
        //
        //         List<Station> queue = busStations.Concat(Enumerable.Reverse(stations).Skip(1).Take(stations.Count - 2)).ToList();
        //         city.Facilities.Add(new Bus(key, queue)
        //         {
        //             Speed = value.Speed,
        //             Behaviour = new ConfigurableFacilityBehaviour(),
        //             Capacity = int.MaxValue,
        //             InfectionProbability = data.BusInfectionProbability ?? 0,
        //             Station = queue.First()
        //         });
        //     }
        //
        //
        //     foreach (Facility facility in city.Facilities.Values)
        //     {
        //         if (!(facility is Station || facility is Bus))
        //         {
        //             foreach (var station in stations)
        //             {
        //                 city.Facilities.Link(station, facility);
        //             }
        //             //Station closest1 = stations.MinBy(x => Point.Distance(x.Coords, facility.Coords));
        //             //city.Facilities.Link(closest1, facility);
        //         }
        //     }
        // }

        public RunConfig Configuration()
        {
            var text = File.ReadAllText(FileName);
            var data = JsonConvert.DeserializeObject<JsonModel>(text);

            return new RunConfig()
            {
                Seed = data.Seed,
                NumThreads = data.NumThreads,
                DeltaTime = Math.Max((int)Math.Round(data.Step * 60 * 24), 1),
                DurationDays = data.TotalTime,
                LogDeltaTime = data.PrintStep.HasValue ? (int?)Math.Max((int)Math.Round(data.PrintStep.Value * 60 * 24), 1) : null,
                TraceDeltaTime = data.TraceStep.HasValue ? (int?)Math.Max((int)Math.Round(data.TraceStep.Value * 60 * 24), 1) : null,
                PrintConsole = data.PrintConsole == 1,
                TraceConsole = data.TraceConsole == 1,
                Params = new ConfigParamsSimple()
                {
                    DeathProbability = data.DeathProbability,
                    IncubationToSpreadDelay = data.IncubationToSpreadDelay,
                    SpreadToImmuneDelay = data.SpreadToImmuneDelay,
                },
            };
        }

    }
}
