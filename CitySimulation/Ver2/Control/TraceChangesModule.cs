using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitySimulation.Control;
using CitySimulation.Entities;
using CitySimulation.Generation.Model2;
using CitySimulation.Health;
using CitySimulation.Tools;
using CitySimulation.Ver2.Entity;
using CitySimulation.Ver2.Entity.Behaviour;

namespace CitySimulation.Ver2.Control
{
    public class TraceChangesModule : Module
    {
        public string Filename;
        public bool PrintConsole;

        private AsyncWriter asyncWriter;


        private int nextLogTime = -1;
        public int LogDeltaTime = 24 * 60;
        public int LogOffset = 8 * 60;

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
                    // {"State", person.HealthData.Infected ? 1 : 0},
                    {"HealthStatus", (int)person.HealthData.HealthStatus},
                });
            }

            File.Delete(Filename);
            asyncWriter = new AsyncWriter(Filename, PrintConsole);
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
                var healthStatus = data[person]["HealthStatus"];

                if (healthStatus != (int)person.HealthData.HealthStatus)
                {
                    lines.Add(GetChangeString(person, "HealthStatus", ((HealthStatus)healthStatus).ToString(), person.HealthData.HealthStatus.ToString()));
                    data[person]["HealthStatus"] = (int)person.HealthData.HealthStatus;
                }

                if (person.Location != null ? person.Location.Id != location : location != int.MinValue)
                {
                    var from = location != int.MinValue ? (FacilityConfigurable)city.Facilities[location] : null;

                    string l1 = from != null ? $"{from.Id} ({from.Type})" : "None";
                    string l2 = person.Location != null ? $"{person.Location.Id} ({((FacilityConfigurable)person.Location).Type})" : "None";

                    lines.Add(GetChangeString(person, "Location", l1, l2));

                    data[person]["Location"] = person.Location?.Id ?? int.MinValue;
                }

                // var infected = data[person]["State"] == 1;
                // if (person.HealthData.Infected != infected)
                // {
                //     lines.Add(GetChangeString(person, "State", infected ? "Infected" : "Healthy", person.HealthData.Infected ? "Infected" : "Healthy"));
                //
                //     data[person]["State"] = person.HealthData.Infected ? 1 : 0;
                // }

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

                asyncWriter.AddLines(lines);
                asyncWriter.AddLine("\n");
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

            lines.Add("\n");

            asyncWriter.AddLines(lines);
        }

        private void PrintInfected()
        {
            List<string> lines = new List<string>()
            {
                "Infected: "
            };

            var city = Controller.City;
            foreach (var group in city.Persons.Where(x=>x.HealthData.Infected)
                .GroupBy(x => ((ConfigurableBehaviour)x.Behaviour).Type))
            {
                lines.Add($"\n{group.Key}: ");
                foreach (var person in @group)
                {
                    lines.Add($"Person id={person.Id} State: Infected");
                }

            }

            if (PrintConsole)
            {
                lines.ForEach(Console.WriteLine);
            }

            lines.Add("\n");

            asyncWriter.AddLines(lines);
        }

        public override void PreRun()
        {
            PrintLocation();
            PrintInfected();
            asyncWriter.Flush();
        }

        public override void Finish()
        {
            PrintLocation();
            PrintInfected();
            asyncWriter.Flush();
        }
    }
}
