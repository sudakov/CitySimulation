using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CitySimulation;
using CitySimulation.Control;
using CitySimulation.Entities;
using CitySimulation.Generation.Model2;
using CitySimulation.Health;
using CitySimulation.Navigation;
using CitySimulation.Tools;
using CitySimulation.Ver2.Control;
using CitySimulation.Ver2.Entity;
using CitySimulation.Ver2.Entity.Behaviour;
using CitySimulation.Ver2.Generation;
using CitySimulation.Ver2.Generation.Osm;

namespace SimulationConsole
{
    class Program
    {
        static void Main(string[] args)
        {

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            //ModelSimple model = new ModelSimple()
            //{
            //    FileName = "UPDESUA.json",
            //    UseTransport = true
            //};

            var model = new OsmModel()
            {
                FileName = "UPDESUA.json",
                UseTransport = true
            };

            RunConfig config = model.Configuration();

            Random random = new Random(config.Seed);

            var time = DateTime.Now;

            City city = model.Generate(random);

            Console.WriteLine($"~~~ Generation time: {(DateTime.Now - time):g} ~~~");

            Controller controller = new ControllerSimple()
            {
                City = city,
                Context = new Context()
                {
                    Random = random,
                    CurrentTime = new CityTime(),
                    Params = config.Params,
                },
                DeltaTime = config.DeltaTime
            };

            Directory.CreateDirectory("output");
            PrintFacilities(city, "output/locations_list.txt");
            PrintPersons(city, "output/person_list.txt");


            KeyValuesWriteModule traceModule = null;

            if (config.TraceDeltaTime.HasValue && config.TraceDeltaTime > 0)
            {
                TraceChangesModule traceChangesModule = new TraceChangesModule()
                {
                    Filename = "output/changes.txt",
                    LogDeltaTime = config.TraceDeltaTime.Value,
                    PrintConsole = config.TraceConsole,
                };

                controller.Modules.Add(traceChangesModule);
            }

            if (config.LogDeltaTime.HasValue && config.LogDeltaTime > 0)
            {
                traceModule = new KeyValuesWriteModule()
                {
                    Filename = "output/table.csv",
                    LogDeltaTime = config.LogDeltaTime.Value,
                    PrintConsole = config.PrintConsole,
                };
                controller.Modules.Add(traceModule);
            }

            if (config.PersonsCountDeltaTime.HasValue && config.PersonsCountDeltaTime > 0)
            {
                PersonsCounterModule personCounterModule = new PersonsCounterModule()
                {
                    Filename = "output/persons_count.csv",
                    LogDeltaTime = config.PersonsCountDeltaTime.Value
                };

                controller.Modules.Add(personCounterModule);
            }


            //Заражаем несколько человек
            // foreach (var person in controller.City.Persons.Take(config.StartInfected))
            // {
            //     person.HealthData.HealthStatus = HealthStatus.InfectedSpread;
            // }

            time = DateTime.Now;

            controller.Setup();

            Console.WriteLine($"~~~ Setup time: {(DateTime.Now - time):g} ~~~");


            controller.OnLifecycleFinished += () =>
            {
                if (controller.Context.CurrentTime.Day >= config.DurationDays)
                {
                    Console.WriteLine("---------------------");
                    Controller.IsRunning = false;
                }
            };
            
            time = DateTime.Now;

            //Запуск симуляции
            controller.RunAsync(config.NumThreads);

            Console.WriteLine($"~~~ Work time: {(DateTime.Now - time):g} ~~~");

            if (traceModule != null)
            {
                var (timeData, data) = traceModule.GetHistory();

                foreach ((string key, List<float> list) in data)
                {
                    var plt = new ScottPlot.Plot(1800, 1200);
                    plt.Title(key);
                    plt.XAxis.DateTimeFormat(true);
                    plt.AddScatter(timeData.Select(x => new DateTime(2000, 1, 1).AddMinutes(x).ToOADate()).ToArray(), list.Select(x => (double)x).ToArray());
                    plt.SaveFig($"output/{key}.png");
                }
            }
        }

        static void PrintFacilities(City city, string filename)
        {
            var lines = new List<string>();

            foreach (var facility in city.Facilities.Values)
            {
                Point? coords = facility is Transport bus ? bus.Station?.Coords : facility.Coords;
                lines.Add($"{facility.Id} - type: {facility.Type}, coords: ({coords})");
            }

            lines.Add("Routes: ");

            List<string> routes = new List<string>();

            foreach (var transport in city.Facilities.Values.OfType<Transport>())
            {
                var route = transport.GetRoute().ToArray();
                var str = string.Join(" -> ", route.Select(x => x.ToLogString()));
                routes.Add(str);
            }

            lines.AddRange(routes.Distinct());

            File.WriteAllLines(filename, lines);
        }

        static void PrintPersons(City city, string filename)
        {
            var lines = new List<string>();

            foreach (var person in city.Persons)
            {
                var behaviour = person.Behaviour as ConfigurableBehaviour;

                lines.Add($"{person.Id} - type: {behaviour?.Type}");
            }

            File.WriteAllLines(filename, lines);
        }
    }
}
