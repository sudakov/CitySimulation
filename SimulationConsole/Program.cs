using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CitySimulation;
using CitySimulation.Control;
using CitySimulation.Control.Modules;
using CitySimulation.Generation.Model2;


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

            var time = DateTime.Now;

            controller.RunAsync(config.NumThreads);

            Console.WriteLine($"~~~ Время работы: {(DateTime.Now - time):g} ~~~");

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

            Console.ReadKey();
            
        }
    }
}
