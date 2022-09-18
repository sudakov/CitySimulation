using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CitySimulation.Behaviour.Action;
using CitySimulation.Control;
using CitySimulation.Entities;
using CitySimulation.Health;
using CitySimulation.Tools;
using CitySimulation.Ver2.Entity.Behaviour;

namespace CitySimulation.Ver2.Control
{
    public class TraceChangesModule : Module
    {
        private enum DataEnum
        {
            NumberOfPersons, Location, HealthStatus, X, Y
        }

        public string Filename;
        public bool PrintConsole;

        private AsyncWriter asyncWriter;

        public int LogDeltaTime = 24 * 60;
        public int LogOffset = 8 * 60;

        private int _nextLogTime = -1;

        private readonly Dictionary<EntityBase, Dictionary<DataEnum, int?>> _data = new();

        public override void Setup(Controller controller)
        {
            base.Setup(controller);
            _nextLogTime = LogOffset;
            if (!(controller is ControllerSimple))
            {
                throw new Exception("ControllerSimple expected");
            }

            _data.Clear();
            asyncWriter?.Close();

            foreach (var facility in controller.City.Facilities.Values)
            {
                _data.Add(facility, new Dictionary<DataEnum, int?>()
                {
                    { DataEnum.NumberOfPersons , facility.PersonsCount }
                });
            }

            foreach (var person in controller.City.Persons)
            {
                _data.Add(person, new Dictionary<DataEnum, int?>()
                {
                    {DataEnum.Location, person.Location?.Id ?? int.MinValue },
                    // {"State", person.HealthData.Infected ? 1 : 0},
                    {DataEnum.HealthStatus, (int)person.HealthData.HealthStatus},
                });
            }

            foreach (var bus in controller.City.Facilities.Values.OfType<Transport>())
            {
                _data[bus].Add(DataEnum.X, null);
                _data[bus].Add(DataEnum.Y, null);
            }

            File.Delete(Filename);
            asyncWriter = new AsyncWriter(Filename);
        }

        public override void PreProcess()
        {
            int totalMinutes = Controller.Context.CurrentTime.TotalMinutes;

            if (_nextLogTime < totalMinutes)
            {
                LogAll();
                _nextLogTime += LogDeltaTime;
            }
        }

        private void LogAll()
        {
            List<string> lines = new List<string>();

            var city = Controller.City;

            foreach (var person in city.Persons)
            {
                var location = _data[person][DataEnum.Location];
                var healthStatus = _data[person][DataEnum.HealthStatus];

                if (healthStatus != (int)person.HealthData.HealthStatus)
                {
                    lines.Add(GetChangeString(person, DataEnum.HealthStatus, ((HealthStatus)healthStatus).ToString(), person.HealthData.HealthStatus.ToString()));
                    _data[person][DataEnum.HealthStatus] = (int)person.HealthData.HealthStatus;
                }

                if (person.Location != null ? person.Location.Id != location : location != int.MinValue)
                {
                    string from = location != int.MinValue ? city.Facilities[location.Value].ToLogString() : null;

                    string l1 = from != null ? from : "None";
                    string l2 = person.Location != null ? person.Location.ToLogString() : "None";

                    lines.Add(GetChangeString(person, DataEnum.Location, l1, l2));

                    _data[person][DataEnum.Location] = person.Location?.Id ?? int.MinValue;
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
                int? count = _data[facility][DataEnum.NumberOfPersons];

                if (facility.PersonsCount != count)
                {

                    lines.Add(GetChangeString(facility, DataEnum.NumberOfPersons, count.ToString(), facility.PersonsCount.ToString()));

                    _data[facility][DataEnum.NumberOfPersons] = facility.PersonsCount;
                }
            }

            foreach (var bus in city.Facilities.Values.OfType<Transport>())
            {
                int? x = null, y = null;
                var station = bus.Station;
                if (station != null)
                {
                    x = station.Coords.X;
                    y = station.Coords.Y;
                    lines.Add($"{bus.ToLogString()} got to {station.ToLogString()}");
                }
                else if (bus.Action is Moving moving)
                {
                    double k = moving.DistanceCovered / moving.Link.Length;
                    x = (int) (moving.Link.From.Coords.X + k * (moving.Link.To.Coords.X - moving.Link.From.Coords.X));
                    y = (int) (moving.Link.From.Coords.Y + k * (moving.Link.To.Coords.Y - moving.Link.From.Coords.Y));
                }

                if (_data[bus][DataEnum.X] != x || _data[bus][DataEnum.Y] != y)
                {
                    var p1 = _data[bus][DataEnum.X] == null || _data[bus][DataEnum.Y] == null ? "None" : $"({_data[bus][DataEnum.X]}, {_data[bus][DataEnum.Y]})";
                    var p2 = x == null || y == null ? "None" : $"({x}, {y})";

                    lines.Add($"{bus.ToLogString()} move {p1} -> {p2}");
                    _data[bus][DataEnum.X] = x;
                    _data[bus][DataEnum.Y] = y;
                }
            }

            if (lines.Any())
            {
                lines.Insert(0, "Time: " + Controller.Context.CurrentTime);

                asyncWriter.AddLines(lines);
                asyncWriter.AddLine("\n");

                if (PrintConsole)
                {
                    lines.ForEach(Console.WriteLine);
                    Console.WriteLine();
                }
            }
        }

        private string GetIncomeString(Person person, string type, long money, string comment)
        {
            return $"Person id={person.Id} {type}: {money} - {comment}";
        }

        private string GetChangeString(Person person, DataEnum param, string from, string to)
        {
            return $"Person id={person.Id} {param}: {from} -> {to}";
        }

        private string GetChangeString(Facility facility, DataEnum param, string from, string to)
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
                var l1 = person.Location != null ? person.Location.ToLogString() : "None";
                lines.Add($"Person id={person.Id} Location: {l1}");
            }

            lines.Add("\n");

            asyncWriter.AddLines(lines);
            if (PrintConsole)
            {
                lines.ForEach(Console.WriteLine);
                Console.WriteLine();
            }
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

            lines.Add("\n");

            asyncWriter.AddLines(lines);

            if (PrintConsole)
            {
                lines.ForEach(Console.WriteLine);
                Console.WriteLine();
            }
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
            asyncWriter?.Close();
        }
    }
}
