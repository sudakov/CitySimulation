using System;
using System.Collections.Generic;
using CitySimulation;
using CitySimulation.Behaviour;
using CitySimulation.Entity;
using CitySimulation.Generation;
using CitySimulation.Generation.Models;
using CitySimulation.Generation.Persons;
using CitySimulation.Tools;

namespace SimulationConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Controller controller = new Controller() { City = new City() };
            City city = controller.City;
            //
            // city.Facilities.Add(new Facility("f1") { Coords = (10, 10) });
            // city.Facilities.Add(new Facility("f2") { Coords = (40, 10) });
            // city.Facilities.Add(new Station("s1") { Coords = (30, 20) });
            // city.Facilities.Add(new Station("s2") { Coords = (80, 20) });
            // city.Facilities.Add(new Station("s3") { Coords = (80, 40) });
            // city.Facilities.Add(new Office("w1") { Coords = (80, 10) });
            // city.Facilities.Add(new Office("w2") { Coords = (80, 50) });
            //
            // city.Facilities.Link("f1", "s1");
            // city.Facilities.Link("f2", "s1");
            // city.Facilities.Link("s1", "s2");
            // city.Facilities.Link("s1", "s3");
            // city.Facilities.Link("s2", "w1");
            // city.Facilities.Link("s3", "w2");
            //
            //
            // city.Facilities.Add(new Bus("b1", new List<Station>()
            // {
            //     city.Get<Station>("s1"),
            //     city.Get<Station>("s2")
            // })
            // { Capacity = 60 });
            //
            // city.Facilities.Add(new Bus("b2", new List<Station>()
            // {
            //     city.Get<Station>("s2"),
            //     city.Get<Station>("s1"),
            //     city.Get<Station>("s3"),
            //     city.Get<Station>("s1"),
            // })
            // { Capacity = 60, Speed = 20 });
            //
            // int count = Controller.TestPersonsCount;
            //
            // for (int i = 0; i < count; i++)
            // {
            //     city.Persons.Add(new Person("p" + i)
            //     {
            //         Behaviour = new PunctualWorkerBehaviour(city.Facilities[i % 2 == 0 ? "f1" : "f2"], city.Facilities[i < count / 2 ? "w1" : "w2"], new TimeRange((i % 3 + 8) * 60, (i % 3 + 8 + 9) * 60))
            //     });
            // }
            //
            // controller.DeltaTime = 1;
            // controller.Setup();
            //
            // controller.Run();

            Model1 model = new Model1()
            {
                Length = 5000,
                DistanceBetweenStations = 500,
                OnFootDistance = 15 * 5,
                Services = new Model1.ServicesConfig()
                {
                    MaxWorkersPerService = 15,
                    ServiceWorkersCount = 2000,
                    ServicesGenerator = new ServicesGenerator()
                    {
                        WorkTime = new TimeRange(8 * 60, 20 * 60),
                        WorkTimeTolerance = 1
                    }
                },
                Areas = new Area[]
                {
                    new ResidentialArea()
                    {
                        Name = "L1",
                        FamiliesPerHouse = 10,
                        HouseSize = 50,
                        HouseSpace = 10,
                        AreaLength = 2000,
                        FamiliesCount = 1000,
                        PersonGenerator = new DefaultPersonGenerator{WorkersPerFamily = 2}
                    },
                    new IndustrialArea()
                    {
                        Name = "I1",
                        HouseSize = 100,
                        HouseSpace = 20,
                        AreaLength = 1000,
                        Offices = new[]
                        {
                            new IndustrialArea.OfficeConfig
                            {
                                WorkersCount = 800,
                                WorkTime = (8*60,17*60)
                            },
                            new IndustrialArea.OfficeConfig
                            {
                                WorkersCount = 12000,
                                WorkTime = (8*60,17*60)
                            },
                            new IndustrialArea.OfficeConfig
                            {
                                WorkersCount = 1000,
                                WorkTime = (8*60,17*60)
                            },
                            new IndustrialArea.OfficeConfig
                            {
                                WorkersCount = 2000,
                                WorkTime = (8*60,17*60)
                            },
                        }
                    }
                    ,
                    new ResidentialArea()
                    {
                    Name = "L2",
                    FamiliesPerHouse = 140,
                    HouseSize = 100,
                    HouseSpace = 20,
                    AreaLength = 2000,
                    FamiliesCount = 5000,
                    PersonGenerator = new DefaultPersonGenerator{WorkersPerFamily = 2}
                    }
                },
                BusesSpeedAndCapacities = new (int, int)[]
                {
                    (500, 350),
                    (500, 350),
                    (500, 350),
                    (500, 350),
                }
            };
            model.Generate(city);

            controller.DeltaTime = 1;
            controller.Setup();

            controller.RunAsync();
        }
    }
}
