using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CitySimulation.Entity;
using CitySimulation.Generation.Model2;
using CitySimulation.Health;
using CitySimulation.Tools;
using Newtonsoft.Json;

namespace CitySimulation.Ver2.Generation
{
    public class Model2
    {
        public string FileName;


        public City Generate(Random random)
        {
            var text = File.ReadAllText(FileName);
            var data = JsonConvert.DeserializeObject<JsonModel>(text);


            var locationGroups = data.LinkLocPeopleTypes.GroupBy(x => x.PeopleType)
                .ToDictionary(x => x.Key, x => x.Select(y=>(y, new List<FacilityConfigurable>())).ToList());


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
                            ConfigurableBehaviour behaviour = new ConfigurableBehaviour()
                            {
                                Type = personTypeFraction.Key,
                                AvailableLocations = locationGroups.GetValueOrDefault(personTypeFraction.Key)
                            };

                            var person = new Person(personTypeFraction.Key + "_" + k)
                            {
                                Behaviour = behaviour,
                                Id = k + PersonIdOffset
                            };

                            k++;

                            person.HealthData = new HealthDataSimple(person);

                            persons.Add(person);

                            person.SetLocation(facility);


                            if (personTypeFraction.Value.Item2.Ispermanent != 0)
                            {
                                behaviour.PersistentFacilities.Add(personTypeFraction.Key, facility);
                            }
                        }
                    }
                }
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

            return city;
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
                TraceDeltaTime = data.TraceStep.HasValue ? (int?)Math.Max((int)Math.Round(data.TraceStep.Value * 60 * 24), 1) : null
            };
        }

    }
}
