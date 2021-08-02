using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CitySimulation;
using CitySimulation.Behaviour;
using CitySimulation.Control;
using CitySimulation.Control.Modules;
using CitySimulation.Entity;
using CitySimulation.Generation;
using CitySimulation.Generation.Model2;
using CitySimulation.Generation.Models;
using CitySimulation.Generation.Persons;
using CitySimulation.Tools;
using DocumentFormat.OpenXml.Drawing.Charts;
using Newtonsoft.Json;
using Range = CitySimulation.Tools.Range;

namespace SimulationConsole
{
    class Program
    {
        static void Main(string[] args)
        {

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            Model2 model = new Model2()
            {
                FileName = "UPDESUA.json"
            };

            RunConfig config = model.Configuration();

            Random random = new Random(config.Seed);

            City city = model.Generate(random);


            Controller controller = new ControllerSimple()
            {
                City = city,
                Context = new Context()
                {
                    Random = random,
                    CurrentTime = new CityTime()
                },
                DeltaTime = config.DeltaTime
            };

            Directory.CreateDirectory("output");

            TraceModule traceModule = null;

            if (config.LogDeltaTime.HasValue)
            {
                traceModule = new TraceModule()
                {
                    Filename = "output/table.csv",
                    LogDeltaTime = config.LogDeltaTime.Value
                };
                controller.Modules.Add(traceModule);
            }
           

            controller.Setup();

            controller.OnLifecycleFinished += () =>
            {
                if (controller.Context.CurrentTime.Day > config.DurationDays)
                {
                    Console.WriteLine("---------------------");
                    Controller.IsRunning = false;
                }
            };

            foreach (var person in controller.City.Persons.Take(2))
            {
                person.HealthData.TryInfect();
            }

            controller.RunAsync(config.NumThreads);

            if (traceModule != null)
            {
                var (timeData, data) = traceModule.GetHistory();

                foreach ((string key, List<float> list) in data)
                {
                    var plt = new ScottPlot.Plot(1800, 1200);
                    plt.XAxis.DateTimeFormat(true);
                    plt.AddScatter(timeData.Select(x => new DateTime(2000, 1, 1).AddMinutes(x).ToOADate()).ToArray(), list.Select(x => (double)x).ToArray());
                    plt.SaveFig($"output/{key}.png");
                }
            }



            // AgesConfig agesConfig = new AgesConfig()
            // {
            //     AdultAge = new Range(18, 65),
            //     WorkerAgeRange = new Range(20, 65),
            //     StudentAgeRange = new Range(2, 20),
            // };
            //
            // ExcelPopulationGenerator generator = new ExcelPopulationGenerator()
            // {
            //     FileName = @"D:\source\repos\CitySimulation\Data\Параметры модели.xlsx",
            //     SheetName = "Доли",
            //     AgentsCount = "F1",
            //     AgeDistributionMale = "E4:E104",
            //     AgeDistributionFemale = "F4:f104",
            //     SingleDistributionMale = "I22:I104",
            //     CountOfFamiliesWith1Children = "R6",
            //     CountOfFamiliesWith2Children = "R5",
            //     CountOfFamiliesWith3Children = "R10",
            //     CountOfFamiliesWith1AndSingleMother = "R8",
            //     AgeConfig = agesConfig
            // };
            // PersonBehaviourGenerator behaviourGenerator = new PersonBehaviourGenerator()
            // {
            //     AgesConfig = agesConfig
            // };
            //
            // var persons = generator.Generate();
            // persons.ForEach(x => behaviourGenerator.GenerateBehaviour(x));
            //
            // var families = persons.Select(x=>x.Family).Distinct().ToList();
            //
            // ExcelPopulationReportWriter reporter = new ExcelPopulationReportWriter()
            // {
            //     FileName = @"D:\source\repos\CitySimulation\Data\Параметры модели.xlsx",
            //     SheetName = "структура популяции",
            //     AgeRange = "A2:A10",
            //     SingleMaleCount = "B2:B10",
            //     FamiliesByMaleAgeCount = "C2:C10",
            //     Families0ChildrenByMaleAgeCount = "D2:D10",
            //     Families1ChildrenByMaleAgeCount = "E2:E10",
            //     Families2ChildrenByMaleAgeCount = "F2:F10",
            //     Families3ChildrenByMaleAgeCount = "G2:G10",
            //     FamiliesWithElderByMaleAgeCount = "H2:H10",
            //     SingleFemaleCount = "I2:I10",
            //     FemaleWith1ChildrenByFemaleAgeCount = "J2:J10",
            //     FemaleWith2ChildrenByFemaleAgeCount = "K2:K10",
            //     FemaleWithElderByFemaleAgeCount = "L2:L10",
            //     AgesCount = new[]
            //     {
            //         ("B18", new Range(0, 7)),
            //         ("B19", new Range(7, 17)),
            //         ("B20:D20", new Range(17, 22)),
            //         ("B21:D21", new Range(22, 65)),
            //         ("B22", new Range(65, 75)),
            //         ("B23", new Range(75, 200)),
            //     },
            // };
            //
            // reporter.WriteReport(persons);
            //
            // Console.WriteLine($"Сгенерированно {persons.Count} человек, {families.Count} семей");
            // Console.ReadKey();


            /*
            Controller controller = new Controller();
            
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
            controller.City = model.Generate();

            controller.DeltaTime = 1;
            controller.Setup();

            controller.RunAsync();*/

        }
    }
}
