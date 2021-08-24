using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CitySimulation.Control;
using CitySimulation.Entities;
using CitySimulation.Generation.Model2;
using CitySimulation.Ver2.Entity;
using CitySimulation.Ver2.Entity.Behaviour;

namespace CitySimulation.Ver2.Control
{
    public class TraceChangesModule : Module
    {
        public string Filename;
        private FileStream stream;


        private int nextLogTime = -1;
        public int LogDeltaTime = 24 * 60;
        public int LogOffset = 8 * 60;
        public bool PrintConsole;

        private Dictionary<EntityBase, Dictionary<string, int>> data = new Dictionary<Entities.EntityBase, Dictionary<string, int>>();

        public override void Setup(Controller controller)
        {
            base.Setup(controller);
            nextLogTime = LogOffset;
            if (!(controller is ControllerSimple))
            {
                throw new Exception("ControllerSimple expected");
            }

            foreach (var facility in controller.City.Facilities.Values)
            {
                data.Add(facility, new Dictionary<string, int>()
                {
                    { "Number of Persons", facility.PersonsCount }
                });
            }

            foreach (var person in controller.City.Persons)
            {
                data.Add(person, new Dictionary<string, int>()
                {
                    {"Location", person.Location?.Id ?? int.MinValue },
                    {"State", person.HealthData.Infected ? 1 : 0}
                });
            }

            File.Delete(Filename);
            stream = File.OpenWrite(Filename);
        }

        public override void PreProcess()
        {
            int totalMinutes = Controller.Context.CurrentTime.TotalMinutes;

            if (nextLogTime < totalMinutes)
            {
                LogAll();
                nextLogTime += LogDeltaTime;
            }
        }

        private void LogAll()
        {
            List<string> lines = new List<string>();

            var city = Controller.City;

            foreach (var person in city.Persons)
            {
                var location = data[person]["Location"];
                if (person.Location != null ? person.Location.Id != location : location != int.MinValue)
                {
                    var from = location != int.MinValue ? (FacilityConfigurable)city.Facilities[location] : null;

                    string l1 = from != null ? $"{from.Id} ({from.Type})" : "None";
                    string l2 = person.Location != null ? $"{person.Location.Id} ({((FacilityConfigurable)person.Location).Type})" : "None";

                    lines.Add(GetChangeString(person, "Location", l1, l2));

                    data[person]["Location"] = person.Location?.Id ?? int.MinValue;
                }

                var infected = data[person]["State"] == 1;

                if (person.HealthData.Infected != infected)
                {
                    lines.Add(GetChangeString(person, "State", infected ? "Infected" : "Healthy", person.HealthData.Infected ? "Infected" : "Healthy"));

                    data[person]["State"] = person.HealthData.Infected ? 1 : 0;
                }

                foreach (var (type, list) in ((ConfigurableBehaviour)person.Behaviour).IncomeHistory)
                {
                    if (list.Count > 0)
                    {
                        foreach (var (money, comment) in list)
                        {
                            lines.Add(GetIncomeString(person, type, money, comment));
                        }

                        list.Clear();
                    }
                }
            }

            foreach (var facility in city.Facilities.Values)
            {
                int count = data[facility]["Number of Persons"];

                if (facility.PersonsCount != count)
                {

                    lines.Add(GetChangeString(facility, "Number of Persons", count.ToString(), facility.PersonsCount.ToString()));

                    data[facility]["Number of Persons"] = facility.PersonsCount;
                }
            }

            if (lines.Any())
            {
                lines.Insert(0, "Time: " + Controller.Context.CurrentTime);

                if (PrintConsole)
                {
                    lines.ForEach(Console.WriteLine);
                }

                stream.WriteAsync(Encoding.UTF8.GetBytes(string.Join('\n', lines) + "\n\n")).AsTask()
                    .ContinueWith(task => stream.Flush());
            }
        }

        private string GetIncomeString(Person person, string type, long money, string comment)
        {
            return $"Person id={person.Id} {type}: {money} - {comment}";
        }

        private string GetChangeString(Person person, string param, string from, string to)
        {
            return $"Person id={person.Id} {param}: {from} -> {to}";
        }

        private string GetChangeString(Facility facility, string param, string from, string to)
        {
            return $"Location id={facility.Id} {param}: {from} -> {to}";
        }

        private void PrintLocation()
        {
            List<string> lines = new List<string>()
            {
                "People locations: "
            };

            var city = Controller.City;
            foreach (var person in city.Persons)
            {
                var l1 = person.Location != null ? $"{person.Location.Id} ({((FacilityConfigurable) person.Location).Type})" : "None";
                lines.Add($"Person id={person.Id} Location: {l1}");
            }
            stream.WriteAsync(Encoding.UTF8.GetBytes(string.Join('\n', lines) + "\n\n")).AsTask()
                .ContinueWith(task => stream.Flush());
        }

        public override void PreRun()
        {
            PrintLocation();
        }

        public override void Finish()
        {
            PrintLocation();

            stream.Flush(true);
            stream?.Close();
        }
    }
}
