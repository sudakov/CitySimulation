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

            List<FacilityConfigurable> facilities = new List<FacilityConfigurable>();

            List<Person> persons = new List<Person>();

            int k = 0;
            int PersonIdOffset = 100000;

            foreach (KeyValuePair<string, LocationType> locationType in data.LocationTypes)
            {
                for (int i = 0; i < locationType.Value.Num; i++)
                {
                    FacilityConfigurable facility = new FacilityConfigurable(locationType.Key + "_" + i)
                    {
                        Type = locationType.Key,
                        InfectionProbability = locationType.Value.InfectionProbability,
                        Behaviour = new ConfigurableFacilityBehaviour()
                    };

                    facilities.Add(facility);

                    int peopleCount = random.RollPuassonInt(locationType.Value.PeopleMean);

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
                                behaviour.PersistentFacilities.Add(personTypeFraction.Key, facility);
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


            int size = 80, space = 20;

            int locationsCount = data.LocationTypes.Sum(x => x.Value.Num);
            int length = (int)Math.Ceiling(Math.Sqrt(locationsCount) * (size + space));
            Point point = new Point(0, space);

            facilities = facilities.Shuffle(random).ToList();

            foreach (var facility in facilities)
            {
                facility.Size = new Point(size, size);
                facility.Coords = new Point(point);

                point.X += size + space;

                if (point.X > length)
                {
                    point.Y += size + space;
                    point.X = 0;
                }

            }
            foreach (var pair in locationGroups)
            {
                foreach (var pair2 in pair.Value)
                {
                    pair2.Item2.AddRange(facilities.Where(x => x.Type == pair2.y.LocationType));
                }
            }

            City city = new City()
            {
                Persons = persons
            };

            city.Facilities.AddRange(facilities);

            if (UseTransport)
            {
                for (int i = 0; i < city.Facilities.Count; i++)
                {
                    for (int j = i + 1; j < city.Facilities.Count; j++)
                    {
                        city.Facilities.Link(city.Facilities[i], city.Facilities[j]);

                        //if (!(city.Facilities[i] is Station) && !(city.Facilities[j] is Bus))
                        //{

                        //    // if (Point.Distance(city.Facilities[i].Coords, city.Facilities[j].Coords) < OnFootDistance)
                        //    // {
                        //    // }
                        //}
                    }
                }

                GenerateBuses(data, city);
            }

            return city;
        }


        private void GenerateBuses(JsonModel data, City city)
        {
            List<Station> stations = new List<Station>();

            foreach ((string key, StationData value) in data.Stations)
            {
                stations.Add(new Station(key)
                {
                    Coords = value.Position,
                    Size = (30,30),
                    Behaviour = new ConfigurableFacilityBehaviour(),
                    InfectionProbability = data.StationInfectionProbability ?? 0
                });
            }

            city.Facilities.AddRange(stations);
            
            for (int i = 0; i < stations.Count; i++)
            {
                for (int j = i + 1; j < stations.Count; j++)
                {
                    city.Facilities.Link(stations[i], stations[j], 0.0000000001);
                }
            }

            foreach ((string key, BusData value) in data.Buses)
            {
                var busStations  = value.Stations.Select(x=>city.Facilities[x]).Cast<Station>().ToList();

                List<Station> queue = busStations.Concat(Enumerable.Reverse(stations).Skip(1).Take(stations.Count - 2)).ToList();
                city.Facilities.Add(new Bus(key, queue)
                {
                    Speed = value.Speed,
                    Behaviour = new ConfigurableFacilityBehaviour(),
                    Capacity = int.MaxValue,
                    InfectionProbability = data.BusInfectionProbability ?? 0,
                    Station = queue.First()
                });
            }


            foreach (Facility facility in city.Facilities.Values)
            {
                if (!(facility is Station || facility is Bus))
                {
                    foreach (var station in stations)
                    {
                        city.Facilities.Link(station, facility);
                    }
                    //Station closest1 = stations.MinBy(x => Point.Distance(x.Coords, facility.Coords));
                    //city.Facilities.Link(closest1, facility);
                }
            }
        }

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
