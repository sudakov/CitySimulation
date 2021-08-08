using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CitySimulation.Generation.Model2;

namespace CitySimulation.Control.Modules
{
    /// <summary>
    /// Модуль отвечает за вывод и запись информации
    /// </summary>
    public class TraceModule : Module
    {
        public string Filename;
        private FileStream stream;

        private Dictionary<string, object> dataToLog = new Dictionary<string, object>();

        private List<int> timeHistory = new List<int>();
        private Dictionary<string, List<float>> history = new Dictionary<string, List<float>>();

        private List<string> keys;

        private int nextLogTime = -1;
        public int LogDeltaTime = 24 * 60;
        public int LogOffset = 12 * 60;


        private List<string> locationTypes;
        public override void Setup(Controller controller)
        {
            base.Setup(controller);
            nextLogTime = LogOffset;
            if (!(controller is ControllerSimple))
            {
                throw new Exception("ControllerSimple expected");
            }

            locationTypes = controller.City.Facilities.Values.Cast<FacilityConfigurable>().Select(x=>x.Type).Distinct().ToList();
            keys = new List<string>();

            keys.AddRange(new string[]
            {
                "Time",
                "Average contacts count per day",
                "Infected count",
                "Uninfected count",
            });

            foreach (string type in locationTypes)
            {
                keys.Add("Count of people in " + type);
            }

            foreach (string type in locationTypes)
            {
                keys.Add("Average stay time in " + type);
            }

            foreach (var key in keys)
            {
                history.Add(key, new List<float>());
            }

            if (Filename != null)
            {
                stream = File.OpenWrite(Filename);
                stream.Write(Encoding.UTF8.GetBytes(String.Join(';', keys) + "\n"));
                stream.Flush();
            }
        }

        public override void PreProcess()
        {
            int totalMinutes = Controller.Context.CurrentTime.TotalMinutes;

            if (nextLogTime < totalMinutes)
            {
                dataToLog.Clear();

                LogAll();

                nextLogTime += LogDeltaTime;
            }
        }

        private void LogAll()
        {
            LogTime(Controller.Context.CurrentTime);

            Dictionary<string, int> personsInLocations = Controller.City.Persons.GroupBy(x => ((FacilityConfigurable)x.Location)?.Type).Where(x => x.Key != null).ToDictionary(x => x.Key, x => x.Count());
            foreach (var type in locationTypes)
            {
                Log("Count of people in " + type, personsInLocations.GetValueOrDefault(type, 0));
            }

            double avg = Controller.City.Persons.Average(x => ((ConfigurableBehaviour)x.Behaviour).GetDayContactsCount());
            int infected = Controller.City.Persons.Count(x => x.HealthData.Infected);
            int nonInfected = Controller.City.Persons.Count - infected;


            Log("Average contacts count per day", (float)avg);
            Log("Infected count", infected);
            Log("Uninfected count", nonInfected);


            Dictionary<string, float> minutesInLocations = Controller.City.Persons.Select(x => ((ConfigurableBehaviour)x.Behaviour).minutesInLocation).SelectMany(d => d) // Flatten the list of dictionaries
                .GroupBy(kvp => kvp.Key, kvp => kvp.Value) // Group the products
                .ToDictionary(g => g.Key, g => g.Average());

            foreach (string type in locationTypes)
            {
                Log("Average stay time in " + type, minutesInLocations.GetValueOrDefault(type, 0));
            }

            FlushLog();
        }

        private void LogTime(CityTime time)
        {
            dataToLog.Add("Time", time.ToString());

            timeHistory.Add(time.TotalMinutes);
        }

        private void Log(string name, string data)
        {
            dataToLog.Add(name, data);
        }

        private void Log(string name, float data)
        {
            dataToLog.Add(name, data);
            history[name].Add(data);
        }

        private void FlushLog()
        {
            Debug.WriteLine("");
            Console.WriteLine();
            foreach (var (name, data) in dataToLog)
            {
                Debug.WriteLine(name + ": " + data);
                Console.WriteLine(name + ": " + data);
            }

            if (stream != null)
            {
                List<string> data = new List<string>();
                foreach (var key in keys)
                {
                    data.Add(dataToLog.GetValueOrDefault(key, "")?.ToString());
                }

                stream.WriteAsync(Encoding.UTF8.GetBytes(string.Join(';', data) + "\n")).AsTask()
                    .ContinueWith(task => stream.FlushAsync());
                
            }



            dataToLog.Clear();
        }

        // public override void Finish()
        // {
        //     LogAll();
        // }

        public (List<int>, Dictionary<string, List<float>>) GetHistory()
        {
            return (timeHistory, history.Where(x=>x.Value.Count > 0).ToDictionary(x=>x.Key, x=>x.Value));
        }
    }
}
