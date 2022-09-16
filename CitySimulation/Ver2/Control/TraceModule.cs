using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CitySimulation.Control;
using CitySimulation.Health;
using CitySimulation.Tools;
using CitySimulation.Ver2.Entity;
using CitySimulation.Ver2.Entity.Behaviour;
using OsmSharp.Db;

namespace CitySimulation.Ver2.Control
{
    /// <summary>
    /// Модуль отвечает за вывод и запись информации
    /// </summary>
    public class KeyValuesWriteModule : Module
    {
        public string Filename;
        private AsyncWriter _asyncWriter;

        public int LogDeltaTime = 24 * 60;
        public int LogOffset = 8 * 60;
        public bool PrintConsole;

        private Dictionary<string, object> _dataToLog = new Dictionary<string, object>();

        private List<int> _timeHistory = new List<int>();
        private Dictionary<string, List<float>> _history = new Dictionary<string, List<float>>();

        private List<string> keys;

        private int _nextLogTime = -1;



        private List<string> _locationTypes;
        private List<string> _incomeItems;
        public override void Setup(Controller controller)
        {
            base.Setup(controller);
            _nextLogTime = LogOffset;
            if (!(controller is ControllerSimple))
            {
                throw new Exception("ControllerSimple expected");
            }

            _locationTypes = controller.City.Facilities.Values.OfType<FacilityConfigurable>().Select(x=>x.Type).Distinct().ToList();
            _incomeItems = controller.City.Persons.Select(x=>x.Behaviour).Cast<ConfigurableBehaviour>().SelectMany(x=>x.Money.Keys).Distinct().ToList();
            keys = new List<string>();
            _history.Clear();
            _asyncWriter?.Close();

            keys.AddRange(new string[]
            {
                "Time",
                "Average contacts count per day",
                // "Infected count",
                // "Uninfected count",
            });

            foreach (string type in _locationTypes)
            {
                keys.Add("Count of people in " + type);
            }

            foreach (string type in _locationTypes)
            {
                keys.Add("Average stay time in " + type);
            }

            foreach (string incomeItem in _incomeItems)
            {
                keys.Add(incomeItem);
            }

            foreach (string healthStatus in Enum.GetNames(typeof(HealthStatus)))
            {
                keys.Add("HealthStatus - " + healthStatus);
            }

            foreach (var key in keys)
            {
                _history.Add(key, new List<float>());
            }

            if (Filename != null)
            {
                _asyncWriter = new AsyncWriter(Filename);
                _asyncWriter.AddLine(String.Join(';', keys));
            }
        }

        public override void PreProcess()
        {
            int totalMinutes = Controller.Context.CurrentTime.TotalMinutes;

            if (_nextLogTime < totalMinutes)
            {
                _dataToLog.Clear();

                LogAll();

                _nextLogTime += LogDeltaTime;
            }
        }

        private void LogAll()
        {
            LogTime(Controller.Context.CurrentTime);

            Dictionary<string, int> personsInLocations = Controller.City.Persons.GroupBy(x => (x.Location as FacilityConfigurable)?.Type).Where(x => x.Key != null).ToDictionary(x => x.Key, x => x.Count());
            foreach (var type in _locationTypes)
            {
                Log("Count of people in " + type, personsInLocations.GetValueOrDefault(type, 0));
            }

            double avg = Controller.City.Persons.Average(x => ((ConfigurableBehaviour)x.Behaviour).GetDayContactsCount());
            // int infected = Controller.City.Persons.Count(x => x.HealthData.HealthStatus == HealthStatus.InfectedIncubation && x.HealthData.HealthStatus == HealthStatus.InfectedSpread);
            // int nonInfected = Controller.City.Persons.Count(x => x.HealthData.HealthStatus == HealthStatus.Susceptible && x.HealthData.HealthStatus == HealthStatus.Recovered);


            Log("Average contacts count per day", (float)avg);
            // Log("Infected count", infected);
            // Log("Uninfected count", nonInfected);

            Dictionary<HealthStatus, int> healthStatuses = Controller.City.Persons.GroupBy(x => x.HealthData.HealthStatus).ToDictionary(x => x.Key, x => x.Count());

            foreach (HealthStatus healthStatus in Enum.GetValues(typeof(HealthStatus)))
            {
                Log("HealthStatus - " + healthStatus, healthStatuses.GetValueOrDefault(healthStatus, 0));
            }

            Dictionary<string, float> minutesInLocations = Controller.City.Persons.Select(x => ((ConfigurableBehaviour)x.Behaviour).MinutesInLocation).SelectMany(d => d) // Flatten the list of dictionaries
                .GroupBy(kvp => kvp.Key, kvp => kvp.Value) // Group the products
                .ToDictionary(g => g.Key, g => g.Average());

            foreach (string type in _locationTypes)
            {
                //Выводим в часах
                Log("Average stay time in " + type, minutesInLocations.GetValueOrDefault(type, 0)/60f);
            }


            Dictionary<string, long> incomeDictionary = Controller.City.Persons.Select(x => x.Behaviour).Cast<ConfigurableBehaviour>()
                .SelectMany(x => x.Money)
                .GroupBy(x => x.Key, x => x.Value)
                .ToDictionary(x=>x.Key, x=>x.Sum());
            
            foreach (var income in incomeDictionary)
            {
                Log(income.Key, income.Value);
            }

            FlushLog();
        }

        private void LogTime(CityTime time)
        {
            _dataToLog.Add("Time", time.ToString());

            _timeHistory.Add(time.TotalMinutes);
        }

        private void Log(string name, string data)
        {
            _dataToLog.Add(name, data);
        }

        private void Log(string name, float data)
        {
            _dataToLog.Add(name, data);
            _history[name].Add(data);
        }

        private void FlushLog()
        {
            if (PrintConsole)
            {
                Debug.WriteLine("");
                Console.WriteLine();
                foreach (var (name, data) in _dataToLog)
                {
                    Debug.WriteLine(name + ": " + data);
                    Console.WriteLine(name + ": " + data);
                }
                Debug.WriteLine("");
                Console.WriteLine();
            }

            if (_asyncWriter != null)
            {
                List<string> data = new List<string>();
                foreach (var key in keys)
                {
                    data.Add(_dataToLog.GetValueOrDefault(key, "")?.ToString());
                }

                _asyncWriter.AddLine(string.Join(';', data));
            }



            _dataToLog.Clear();
        }

        public override void Finish()
        {
            _asyncWriter?.Close();
        }

        public (List<int>, Dictionary<string, List<float>>) GetHistory()
        {
            return (_timeHistory, _history.Where(x=>x.Value.Count > 0).ToDictionary(x=>x.Key, x=>x.Value));
        }
    }
}
