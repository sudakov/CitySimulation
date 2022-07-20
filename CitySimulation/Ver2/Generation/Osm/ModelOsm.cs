using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CitySimulation.Entities;
using CitySimulation.Generation.Model2;
using CitySimulation.Health;
using CitySimulation.Tools;
using CitySimulation.Ver2.Entity.Behaviour;
using CitySimulation.Ver2.Entity;
using Newtonsoft.Json;
using OsmSharp.Streams;
using CitySimulation.Ver2.Control;
using OsmSharp;

namespace CitySimulation.Ver2.Generation.Osm
{
    public class OsmModel
    {
        const int SCALE = 100000;

        public string FileName { get; set; }
        public bool UseTransport { get; set; }


        public City Generate(Random random)
        {
            var jsonModel = LoadJson(FileName);
            jsonModel.LinkLocPeopleTypes.ForEach(x => x.Income ??= new List<Income>(0));

            Dictionary<string, List<(LinkLocPeopleType y, List<FacilityConfigurable>)>> locationGroups = jsonModel.LinkLocPeopleTypes.GroupBy(x => x.PeopleType)
                .ToDictionary(x => x.Key, x => x.Select(y => (y, new List<FacilityConfigurable>())).ToList());


            var (facilities, persons) = CreateFacilitiesAndPersons(random, jsonModel, locationGroups);


            var param = new ConfigParamsSimple()
            {
                DeathProbability = jsonModel.DeathProbability,
                IncubationToSpreadDelay = jsonModel.IncubationToSpreadDelay,
                SpreadToImmuneDelay = jsonModel.SpreadToImmuneDelay,
            };

            CityTime time = new CityTime();

            InfectPersons(random, jsonModel, persons, param, time);

            foreach (var pair in locationGroups)
            {
                foreach (var pair2 in pair.Value)
                {
                    pair2.Item2.AddRange(facilities.OfType<FacilityConfigurable>().Where(x => x.Type == pair2.y.LocationType));
                }
            }

            City city = new City()
            {
                Persons = persons,
                Facilities = new FacilityManager(facilities)
            };

            return city;
        }

        private (List<Facility> facilities, List<Person> persons) CreateFacilitiesAndPersons(Random random, OsmJsonModel jsonModel, 
            Dictionary<string, List<(LinkLocPeopleType y, List<FacilityConfigurable>)>> locationGroups)
        {

            List<Facility> facilities = new List<Facility>();

            List<Person> persons = new List<Person>();


            Dictionary<long, Point> nodes = new Dictionary<long, Point>();

            using (var source = new XmlOsmStreamSource(new FileInfo(jsonModel.OsmFilename).OpenRead()))
            {
                foreach (var element in source)
                {
                    if (element is Node node)
                    {
                        nodes.Add(node.Id.Value, new Point((int)(node.Longitude * SCALE), (int)(node.Latitude * SCALE)));
                    }
                }
            }

            using (var source = new XmlOsmStreamSource(new FileInfo(jsonModel.OsmFilename).OpenRead()))
            {
                int k = 0;
                int PersonIdOffset = 100000;

                int i = 0;
                foreach (var element in source)
                {
                    var buildingType = element.Tags?.GetOrDefault("building");
                    var buildingName = element.Tags?.GetOrDefault("name");
                    if (buildingType != null && buildingName != null)
                    {
                        Debug.WriteLine(buildingType + ": " + buildingName);
                        // if (buildingType == "commercial")
                        // {
                        //     commercials.Add(element);
                        // }
                        // if (buildingType == "apartments")
                        // {
                        //     var a = 3;
                        // }
                        var locationType = jsonModel.LocationTypes.FirstOrDefault(x => x.Value.OsmTags.Contains(buildingType));
                        
                        if (locationType.Value != null)
                        {
                            Facility facility;
                            if (jsonModel.TransportStationLinks?.Any(x => x.StationType == locationType.Key) == true)
                            {
                                facility = new Station(locationType.Key + ": " + buildingName + "-" + i++);
                            }
                            else
                            {
                                facility = new FacilityConfigurable(locationType.Key + ": " + buildingName + "-" + i++);
                            }

                            facility.Type = locationType.Key;
                            facility.InfectionProbability = locationType.Value.InfectionProbability;
                            facility.Behaviour = new ConfigurableFacilityBehaviour();

                            LocateFacility(facility, element, nodes);


                            facilities.Add(facility);

                            int peopleCount = locationType.Value.PeopleMean == 0
                                ? 0
                                : random.RollPuassonInt(locationType.Value.PeopleMean);

                            var personTypeFractions = jsonModel.PeopleTypes
                                .ToDictionary(x => x.Key,
                                    x => (x.Value,
                                        jsonModel.LinkLocPeopleTypes.FirstOrDefault(y =>
                                            y.LocationType == locationType.Key && x.Key == y.PeopleType)));

                            double sumWeight = personTypeFractions.Where(x => x.Value.Item2 != null)
                                .Sum(x => x.Value.Value.Fraction);

                            foreach (var personTypeFraction in personTypeFractions.Where(x => x.Value.Item2 != null))
                            {
                                int count = (int)Math.Round(peopleCount * personTypeFraction.Value.Value.Fraction / sumWeight);
                                for (int j = 0; j < count; j++)
                                {
                                    ConfigurableBehaviour behaviour = UseTransport
                                        ? new ConfigurableBehaviourWithTransport()
                                        : new ConfigurableBehaviour();

                                    behaviour.Type = personTypeFraction.Key;
                                    behaviour.AvailableLocations = locationGroups.GetValueOrDefault(personTypeFraction.Key);

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
                }

            }

            return (facilities, persons);
        }

        private void LocateFacility(Facility facility, OsmGeo element, Dictionary<long, Point> nodes)
        {
            if (element is Way way)
            {
                Point point = way.Nodes.Aggregate(new Point(), (point, l) => point + nodes[l]) / way.Nodes.Length;
                facility.Coords = point;

                facility.Polygon = way.Nodes.Select(x => nodes[x]).ToArray();
            }
            else if(element is Node node)
            {
                facility.Coords = nodes[node.Id.Value];
            }
        }


        private void InfectPersons(Random random, OsmJsonModel jsonModel, List<Person> persons, ConfigParamsSimple param, CityTime time)
        {
            foreach (var (key, peopleType) in jsonModel.PeopleTypes)
            {
                persons.Where(x => ((ConfigurableBehaviour)x.Behaviour).Type == key).Shuffle(random)
                    .Take(peopleType.StartInfected).ToList()
                    .ForEach(x => (x.HealthData as HealthDataSimple).TryInfect(param, time, random));
            }
        }

        public RunConfig Configuration()
        {
            var text = File.ReadAllText(FileName);
            var data = JsonConvert.DeserializeObject<OsmJsonModel>(text);

            return new RunConfig()
            {
                Seed = data.Seed,
                NumThreads = data.NumThreads,
                DeltaTime = Math.Max((int)Math.Round(data.Step * 60 * 24), 1),
                DurationDays = data.TotalTime,
                LogDeltaTime = data.PrintStep.HasValue && data.PrintStep > 0 ? (int?)Math.Max((int)Math.Round(data.PrintStep.Value * 60 * 24), 1) : null,
                TraceDeltaTime = data.TraceStep.HasValue && data.TraceStep > 0 ? (int?)Math.Max((int)Math.Round(data.TraceStep.Value * 60 * 24), 1) : null,
                PersonsCountDeltaTime = data.PersonsCountStep.HasValue && data.PersonsCountStep > 0 ? (int?)Math.Max((int)Math.Round(data.PersonsCountStep.Value * 60 * 24), 1) : null,
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

        public Dictionary<string, string> FacilityColors()
        {
            var data = LoadJson(FileName);

            return data.LocationTypes.ToDictionary(x => x.Key, x => x.Value.Color);
        }

        public List<string> FacilityTypes()
        {
            var data = LoadJson(FileName);
            
            return data.LocationTypes.Keys.ToList();
        }

        private OsmJsonModel LoadJson(string fileName)
        {
            return JsonConvert.DeserializeObject<OsmJsonModel>(File.ReadAllText(fileName));
        }
    }
}
