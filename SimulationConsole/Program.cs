using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CitySimulation;
using CitySimulation.Control;
using CitySimulation.Generation.Model2;
using CitySimulation.Health;
using CitySimulation.Ver2.Control;
using CitySimulation.Ver2.Entity;
using CitySimulation.Ver2.Entity.Behaviour;
using CitySimulation.Ver2.Generation;


namespace SimulationConsole
{
    class Program
    {
        static void Main(string[] args)
        {

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            ModelSimple model = new ModelSimple()
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

            //Заражаем несколько человек
            // foreach (var person in controller.City.Persons.Take(config.StartInfected))
            // {
            //     person.HealthData.HealthStatus = HealthStatus.InfectedSpread;
            // }

            controller.Setup();

            controller.OnLifecycleFinished += () =>
            {
                if (controller.Context.CurrentTime.Day >= config.DurationDays)
                {
                    Console.WriteLine("---------------------");
                    Controller.IsRunning = false;
                }
            };
            
            var time = DateTime.Now;

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

            foreach (var facility in city.Facilities.Values.Cast<FacilityConfigurable>())
            {
                lines.Add($"{facility.Id}: {facility.Type}");
            }

            File.WriteAllLines(filename, lines);
        }

        static void PrintPersons(City city, string filename)
        {
            var lines = new List<string>();

            foreach (var person in city.Persons)
            {
                var behaviour = person.Behaviour as ConfigurableBehaviour;

                lines.Add($"{person.Id}: {behaviour?.Type}");
            }

            File.WriteAllLines(filename, lines);
        }
    }
}
