using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Accessibility;
using CitySimulation;
using CitySimulation.Behaviour;
using CitySimulation.Control.Log;
using CitySimulation.Control.Log.DbModel;
using CitySimulation.Entity;
using CitySimulation.Generation;
using CitySimulation.Generation.Models;
using CitySimulation.Generation.Persons;
using CitySimulation.Tools;
using GraphicInterface.Render;
using LiteDB;
using LiveCharts.WinForms;

namespace GraphicInterface
{
    public partial class Form1 : Form
    {
        private Controller controller;

        public RenderParams RenderParams = new RenderParams(){Scale = 0.38f, FacilitySize = 30};

        private Dictionary<Type, Renderer> renderers = new Dictionary<Type, Renderer>()
        {
            {typeof(Station), new FacilityRenderer(){Brush = Brushes.Red} },
            {typeof(Office), new FacilityRenderer(){Brush = Brushes.Blue} },
            {typeof(LivingHouse), new FacilityRenderer(){Brush = Brushes.Yellow} },
            {typeof(Service), new FacilityRenderer(){Brush = Brushes.LawnGreen} },
            {typeof(Bus), new BusRenderer(){Brush = Brushes.Cyan, WaitingBrush = Brushes.DarkCyan} }
        };

        private PersonsRenderer personsRenderer = new PersonsRenderer();

        public Form1()
        {
            InitializeComponent();
            controller = new Controller(){City = new City()};
            Init();
            controller.OnLifecycleFinished += Controller_OnLifecycleFinished;
            
            panel1.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(panel1, true);

        }

        private void Controller_OnLifecycleFinished()
        {
            if (stop_minutes.HasValue)
            {
                stop_minutes -= controller.DeltaTime;
                if (stop_minutes <= 0)
                {
                    stop_minutes = null;
                    Controller.Paused = true;
                }
            }
        }

        private void Init()
        {
            City city = controller.City;
            
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
            // }){ Capacity = 60});
            //
            // city.Facilities.Add(new Bus("b2", new List<Station>()
            // {
            //     city.Get<Station>("s2"),
            //     city.Get<Station>("s1"),
            //     city.Get<Station>("s3"),
            //     city.Get<Station>("s1"),
            // }){ Capacity = 60, Speed = 20});
            //
            // RenderParams.Scale = 10;
            // int count = 1000;
            //
            // for (int i = 0; i < count; i++)
            // {
            //     city.Persons.Add(new Person("p" + i)
            //     {
            //         Behaviour = new PunctualWorkerBehaviour(
            //             city.Facilities[i % 2 == 0 ? "f1" : "f2"],
            //             city.Facilities[i < count / 2 ? "w1" : "w2"], 
            //             new TimeRange((i % 3 + 8) * 60, (i % 3 + 8 + 9) * 60))
            //     });
            // }

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
                        WorkTime = new TimeRange(8*60, 20*60),
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

            controller.DeltaTime = (int)numericUpDown1.Value;
            controller.Setup();
        }
        
        
        private void StartSimulation()
        {
            Task.Run(() =>
            {
                int? sessionId = controller.RunAsync();
                Debug.WriteLine("Session Id: " + sessionId);

                if (sessionId.HasValue)
                {
                    if (Controller.Logger is DBLogger logger)
                    {
                        var collection = logger.CreateConnection().GetCollection<PersonInFacilityTime>();
                        // int count = collection.Query().Where(x=>x.SessionId == sessionId).Count();
                        // List<PersonInFacilityTime> data = collection.Query().Where(x => x.SessionId == sessionId && x.Person == "p1").Limit(100).ToList();
                        // File.WriteAllLines("last_session_data.txt", data.Select(x=>
                        //     $"{new LogCityTime(x.StartDay, x.StartMin)} - {new LogCityTime(x.EndDay, x.EndMin)}: {x.Person} -> {x.Facility}"
                        // ));
                        // Debug.WriteLine("Log sample saved");

                        string facilityName = "S_3";
                        var data = collection.Query()
                            .Where(x=>x.Facility == facilityName)
                            .ToList();

                        List<(int, int)> personInFacilityTimes = 
                            data.Select(x =>
                                (x.StartDay * 24 * 60 + x.StartMin, x.EndDay * 24 * 60 + x.StartMin)).ToList();

                        int maxTime = personInFacilityTimes.Max(x => x.Item2);

                        int delta = 5;
                        int halfDelta = delta / 2;

                        ConcurrentBag<(int,int)> countData = new ConcurrentBag<(int, int)>();

                        OrderablePartitioner<Tuple<int, int>> rangePart = Partitioner.Create(0, maxTime / delta);

                        Parallel.ForEach(rangePart, (range, loopState) =>
                        {
                            var personInFacilityTimesClone = personInFacilityTimes
                                .Where(x => x.Item1 + halfDelta > range.Item1 && x.Item2 - halfDelta < range.Item2).ToList();

                            int prevCount = 0;
                            for (int i = range.Item1; i < range.Item2; i++)
                            {
                                int time = i * delta;
                                int count = 0;
                                for (int j = 0; j < personInFacilityTimesClone.Count; j++)
                                {
                                    if (personInFacilityTimesClone[j].Item1 - halfDelta < time &&
                                        personInFacilityTimesClone[j].Item2 + halfDelta > time)
                                    {
                                        count++;
                                    }

                                    if (count != prevCount)
                                    {
                                        prevCount = count;
                                        countData.Add((time, count));
                                        personInFacilityTimesClone.RemoveAll(x => x.Item2 + halfDelta < time);
                                    }
                                }
                            }
                        });

                        List<(int, int)> sortedCountData = countData.OrderBy(x=>x.Item1).ToList();
                        // int prevCount = 0;
                        // for (int i = 0; i < maxTime; i += delta)
                        // {
                        //     int count = 0;
                        //     for (int j = 0; j < personInFacilityTimes.Count; j++)
                        //     {
                        //         if (personInFacilityTimes[j].Item1 - halfDelta < i &&
                        //             personInFacilityTimes[j].Item2 + halfDelta > i)
                        //         {
                        //             count++;
                        //         }
                        //     }
                        //     if (count != prevCount)
                        //     {
                        //         prevCount = count;
                        //         countData.Add((i, count));
                        //         personInFacilityTimes.RemoveAll(x => x.Item2 + halfDelta < i);
                        //     }
                        // }
                        //
                        // int a = 0;
                    }
                }
            });
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            panel1.Invalidate();
            time_label.Text = Controller.CurrentTime.ToString();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            // e.Graphics.Clear(Color.Black);
            City city = Controller.Instance.City;

            foreach (Facility facility in city.Facilities.Values)
            {
                renderers[facility.GetType()].Render(facility, e.Graphics, RenderParams);
            }

            personsRenderer.Render(city.Persons, e.Graphics, RenderParams);

        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            controller.DeltaTime = (int)((NumericUpDown) sender).Value;
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            controller.SleepTime = (int)((NumericUpDown)sender).Value * 10;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Controller.Paused = false;
            if (!Controller.IsRunning)
            {
                StartSimulation();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Controller.IsRunning = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Controller.Paused = true;
        }

        private int? stop_minutes = null;
        private void button4_Click(object sender, EventArgs e)
        {
            string[] split = stop_textBox.Text.Split(":");

            stop_minutes = int.Parse(split[0]) * 60 + int.Parse(split[1]) - Controller.CurrentTime.Minutes;
            if (stop_minutes <= 0)
            {
                stop_minutes += 24 * 60;
            }
        }
    }
}
