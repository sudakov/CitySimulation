using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using CitySimulation.Control;
using CitySimulation.Control.Log;
using CitySimulation.Control.Log.DbModel;
using CitySimulation.Entity;
using CitySimulation.Generation;
using CitySimulation.Generation.Areas;
using CitySimulation.Generation.Models;
using CitySimulation.Generation.Persons;
using CitySimulation.Tools;
using GraphicInterface.Render;
using Module = CitySimulation.Control.Module;
using Point = System.Drawing.Point;
using Range = CitySimulation.Tools.Range;

namespace GraphicInterface
{
    public partial class Form1 : Form
    {
        private Controller controller;

        private Dictionary<Type, Renderer> renderers = new Dictionary<Type, Renderer>()
        {
            {typeof(Station), new FacilityRenderer(){Brush = Brushes.Red} },
            {typeof(Office), new FacilityRenderer(){Brush = Brushes.Blue} },
            {typeof(LivingHouse), new FacilityRenderer(){Brush = Brushes.Yellow} },
            {typeof(Service), new FacilityRenderer(){Brush = Brushes.LawnGreen} },
            {typeof(RecreationService), new FacilityRenderer(){Brush = Brushes.LawnGreen} },
            {typeof(School), new FacilityRenderer(){Brush = Brushes.DarkGreen} },
            {typeof(Bus), new BusRenderer(){Brush = Brushes.Cyan, WaitingBrush = Brushes.DarkCyan} }
        };

        private PersonsRenderer personsRenderer = new PersonsRenderer();

        #region DataSelectors

        

        private List<Func<string>> commonDataSelector;


        private List<Func<Facility, string>> facilitiesDataSelector;
        private List<Func<Facility, Brush>> facilitiesColorSelector;
        #endregion


        private ImmutableDictionary<Facility, IEnumerable<Person>> _facilityPersons;
        private ImmutableDictionary<Facility, IEnumerable<Person>> FacilityPersons => _facilityPersons;



        private Dictionary<RealtimePlotForm, Func<(int, int)>> plots = new Dictionary<RealtimePlotForm, Func<(int, int)>>();

        public Form1()
        {
            InitializeComponent();

            commonDataSelector = new List<Func<string>>()
            {
                ()=> "Инкубация: " + controller.City.Persons.Count(x=>x.HealthData.HealthStatus == HealthStatus.InfectedIncubation),
                ()=> "Расспространение: " + controller.City.Persons.Count(x=>x.HealthData.HealthStatus == HealthStatus.InfectedSpread),
                ()=> "С иммунитетом: " + controller.City.Persons.Count(x=>x.HealthData.HealthStatus == HealthStatus.Immune),
            };

            facilitiesDataSelector = new List<Func<Facility, string>>()
            {
                facility => facility.PersonsCount.ToString(),
                facility => (FacilityPersons.GetValueOrDefault(facility, null)?.Count(x => x.Age < 18) ?? 0).ToString(),
                facility => (FacilityPersons.GetValueOrDefault(facility, null)?.Count(x=>x.Age >= 60) ?? 0).ToString(),
                facility =>
                {
                    int spread = FacilityPersons.GetValueOrDefault(facility, null)?.Count(x=>x.HealthData.HealthStatus == HealthStatus.InfectedSpread) ?? 0;
                    int incub = FacilityPersons.GetValueOrDefault(facility, null)?.Count(x=>x.HealthData.HealthStatus == HealthStatus.InfectedIncubation) ?? 0;
                    return spread + "/" + incub;
                }
            };

            facilitiesColorSelector = new List<Func<Facility, Brush>>()
            {
                null,
                null,
                null,
                facility =>
                {
                    bool spread = FacilityPersons.GetValueOrDefault(facility, null)?.Any(x => x.HealthData.HealthStatus == HealthStatus.InfectedSpread) == true;
                    bool incub = FacilityPersons.GetValueOrDefault(facility, null)?.Any(x => x.HealthData.HealthStatus == HealthStatus.InfectedIncubation) == true;
                    return new SolidBrush(Color.FromArgb(spread || incub ? 255 : 0, incub && !spread ? 255 : 0, 0));
                },
            };


            comboBox1.SelectedIndex = 0;

            controller = new Controller()
            {
                VirusSpreadModule = new VirusSpreadModule(),
                Logger = new FacilityPersonsCountLogger(),
            };


            Generate();
            controller.OnLifecycleFinished += Controller_OnLifecycleFinished;
            
            panel1.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(panel1, true);
            panel1.MouseWheel += Panel1OnMouseWheel;

        }

        private void OpenPlotFor(string title, Func<(int, int)> func)
        {
            var form = new RealtimePlotForm(title);
            form.Show();
            plots.Add(form, func);
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

        private City Generate()
        {
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

            // Model1 model = new Model1()
            // {
            //     Length = 5000,
            //     DistanceBetweenStations = 500,
            //     OnFootDistance = 15 * 5,
            //     Services = new ServicesConfig()
            //     {
            //         MaxWorkersPerService = 15,
            //         ServiceWorkersCount = 2000,
            //         ServicesGenerator = new ServicesGenerator()
            //         {
            //             WorkTime = new TimeRange(8*60, 20*60),
            //             WorkTimeTolerance = 1
            //         }
            //     },
            //     Areas = new Area[]
            //     {
            //         new ResidentialArea()
            //         {
            //             Name = "L1",
            //             FamiliesPerHouse = 10,
            //             HouseSize = 50,
            //             HouseSpace = 10,
            //             AreaDepth = 2000,
            //             FamiliesCount = 1000,
            //             PersonGenerator = new DefaultPersonGenerator{WorkersPerFamily = 2}
            //         },
            //         new IndustrialArea()
            //         {
            //             Name = "I1",
            //             HouseSize = 100,
            //             HouseSpace = 20,
            //             AreaLength = 1000,
            //             Offices = new[]
            //             {
            //                 new IndustrialArea.OfficeConfig
            //                 {
            //                     WorkersCount = 800,
            //                     WorkTime = (8*60,17*60)
            //                 },
            //                 new IndustrialArea.OfficeConfig
            //                 {
            //                     WorkersCount = 12000,
            //                     WorkTime = (8*60,17*60)
            //                 },
            //                 new IndustrialArea.OfficeConfig
            //                 {
            //                     WorkersCount = 1000,
            //                     WorkTime = (8*60,17*60)
            //                 },
            //                 new IndustrialArea.OfficeConfig
            //                 {
            //                     WorkersCount = 2000,
            //                     WorkTime = (8*60,17*60)
            //                 },
            //             }
            //         }
            //         ,
            //         new ResidentialArea()
            //         {
            //         Name = "L2",
            //         FamiliesPerHouse = 140,
            //         HouseSize = 100,
            //         HouseSpace = 20,
            //         AreaDepth = 2000,
            //         FamiliesCount = 5000,
            //         PersonGenerator = new DefaultPersonGenerator{WorkersPerFamily = 2}
            //         }
            //     },
            //     BusesSpeedAndCapacities = new (int, int)[]
            //     {
            //         (500, 350),
            //         (500, 350),
            //         (500, 350),
            //         (500, 350),
            //     }
            // };

            House h9 = new House()
            {
                Name = "Девятиэтажный",
                Size = (int) Math.Ceiling(Math.Sqrt(0.4 * 10000)),
                FamiliesPerHouse = 140
            };
            House h5 = new House()
            {
                Name = "Пятиэтажный",
                Size = (int)Math.Ceiling(Math.Sqrt(0.3 * 10000)),
                FamiliesPerHouse = 80
            };
            House h3 = new House()
            {
                Name = "Трёхэтажный",
                Size = (int)Math.Ceiling(Math.Sqrt(0.3 * 10000)),
                FamiliesPerHouse = 24
            };

            House h1 = new House()
            {
                Name = "Частный",
                Size = (int)Math.Ceiling(Math.Sqrt(0.1 * 10000)),
                FamiliesPerHouse = 1
            };

            ExcelPopulationGenerator personsGenerator = new ExcelPopulationGenerator()
            {
                FileName = @"D:\source\repos\CitySimulation\Data\Параметры модели.xlsx",
                SheetName = "Доли",
                AgentsCount = "F1",
                AgeDistributionMale = "E4:E104",
                AgeDistributionFemale = "F4:f104",
                SingleDistributionMale = "I22:I104",
                CountOfFamiliesWith1Children = "R6",
                CountOfFamiliesWith2Children = "R5",
                CountOfFamiliesWith3Children = "R10",
                CountOfFamiliesWith1AndSingleMother = "R8",
            };

            Model1 model = new Model1()
            {
                DistanceBetweenStations = 500,
                AreaSpace = 200,
                Areas = new Area[]
                {
                    new IndustrialArea()
                    {
                        Name = "I1",
                        WorkplacesRatio = 0.35f,
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
                                WorkersCount = 1200,
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
                    },
                    new AdministrativeArea()
                    {
                        Name = "Adm",
                        WorkplacesRatio = 0.25f,
                        AreaDepth = 600,
                        HouseSpace = 100,
                        Service = new []
                        {
                            new Service("МФЦ")
                            {
                                Size = new CitySimulation.Tools.Point(100, 100),
                                WorkTime = new Range(8 * 60, 17 * 60),
                            },
                            new Service("ПРФ")
                            {
                                Size = new CitySimulation.Tools.Point(100, 100),
                                WorkTime = new Range(8 * 60, 17 * 60),
                            },
                            new Service("ФНС")
                            {
                                Size = new CitySimulation.Tools.Point(100, 100),
                                WorkTime = new Range(8 * 60, 17 * 60),
                            },
                            new Service("ФСС")
                            {
                                Size = new CitySimulation.Tools.Point(100, 100),
                                WorkTime = new Range(8 * 60, 17 * 60),
                            },
                            new Service("Военкомат")
                            {
                                Size = new CitySimulation.Tools.Point(100, 100),
                                WorkTime = new Range(8 * 60, 17 * 60),
                            },
                        }
                    }, 
                    new ResidentialArea()
                    {
                        Name = "Old",
                        HouseSpace = 5,
                        Houses = new []{ h3,h5 },
                        AreaDepth = (int)(0.6 * 1000),
                        HousesDistribution = new []{0.5,0.5},
                        SchoolDistance = (300, 600),
                        FamiliesPerSchool = 892,
                    },
                    new ResidentialArea()
                    {
                        Name = "Private",
                        HouseSpace = 5,
                        Houses = new []{ h1 },
                        AreaDepth = (int)(0.6 * 1000),
                        SchoolDistance = (300, 600),
                        FamiliesPerSchool = 892,
                    },
                    new ResidentialArea()
                    {
                        Name = "New",
                        HouseSpace = 5,
                        Houses = new []{ h9 },
                        AreaDepth = (int)(0.6 * 1000),
                        SchoolDistance = (300, 600),
                        FamiliesPerSchool = 892,
                    },
                },

                ServicesConfig = new ServicesConfig
                {
                    ServiceWorkersRatio = 0.4f,
                    LocalWorkersRatio = 0.41f,
                    ServicesData = new List<ServicesConfig.ServiceDataBase>()
                    {
                        new ServicesConfig.ServiceData<Service>("парикмахерская", "ремонт")
                        {
                            Prefix = "MinServ",
                            WorkerTime = new Range(8*60, 17 * 60),
                            WorkersPerService = new Range(1, 15),
                            FamiliesPerService = 250,
                        },
                        new ServicesConfig.ServiceData<Service>("маркет")
                        {
                            Prefix = "Store",
                            WorkerTime = new Range(8*60, 17 * 60),
                            WorkersPerService = new Range(15, 30),
                            FamiliesPerService = 1000
                        },
                        new ServicesConfig.ServiceData<RecreationService>("вечерние курсы", "клуб", "бассейн", 
                            "спортзал", "стадион", "ресторан", "пивбар")
                        {
                            Prefix = "Recreation",
                            WorkerTime = new Range(8*60, 17 * 60),
                            WorkersPerService = new Range(5, 25),
                            FamiliesPerService = 10000/3
                        }
                    },
                },
                BusesSpeedAndCapacities = new (int, int)[]
                {
                    (500, 350),
                    (500, 350),
                    (500, 350),
                    (500, 350),
                },
            };

            PersonBehaviourGenerator behaviourGenerator = new PersonBehaviourGenerator()
            {
                WorkerAgeRange = new Range(20, 65),
                StudentAgeRange = new Range(2, 20),
            };

            //Генерируем население
            List<Person> persons = personsGenerator.Generate();
            List<Family> families = persons.Select(x=>x.Family).Distinct().ToList();

            //Настраиванием поведение
            persons.ForEach(x=> behaviourGenerator.GenerateBehaviour(x));

            var familiesPerLivingArea = new Dictionary<string, int>()
            {
                {"Old", families.Count - (int) (0.1 * families.Count) - (int) (0.4 * families.Count)},
                {"Private", (int) (0.1 * families.Count)},
                {"New", (int) (0.4 * families.Count)}
            };

            //Генерируем город
            controller.City = model.Generate(familiesPerLivingArea);

            //Заселяем город (устраиваем на работу)
            model.Populate(familiesPerLivingArea, families.Shuffle(Controller.Random));

            controller.DeltaTime = (int)numericUpDown1.Value;
            controller.Setup();

            {
                ExcelPopulationReportWriter reporter = new ExcelPopulationReportWriter()
                {
                    FileName = @"D:\source\repos\CitySimulation\Data\Параметры модели.xlsx",
                    SheetName = "структура популяции",
                    AgeRange = "A2:A10",
                    SingleMaleCount = "B2:B10",
                    FamiliesByMaleAgeCount = "C2:C10",
                    Families0ChildrenByMaleAgeCount = "D2:D10",
                    Families1ChildrenByMaleAgeCount = "E2:E10",
                    Families2ChildrenByMaleAgeCount = "F2:F10",
                    Families3ChildrenByMaleAgeCount = "G2:G10",
                    FamiliesWithElderByMaleAgeCount = "H2:H10",
                    SingleFemaleCount = "I2:I10",
                    FemaleWith1ChildrenByFemaleAgeCount = "J2:J10",
                    FemaleWith2ChildrenByFemaleAgeCount = "K2:J10",
                    FemaleWithElderByFemaleAgeCount = "L2:L10",
                    AgesCount = new []
                    {
                        ("B18", new Range(0, 7)),
                        ("B19", new Range(7, 17)),
                        ("B20:B22", new Range(17, 22)),
                        ("B21", new Range(22, 65)),
                        ("B22", new Range(65, 75)),
                        ("B23", new Range(75, 200)),
                    },
                };
                reporter.WriteReport(persons);
            }

            return controller.City;
        }

        private void TestSimulation()
        {
            Task.Run(() =>
            {
                Controller.Random = new Random(0);
                Generate();
                Controller.Random = new Random(0);
                controller.Run(10000);

                int[] data = new int[0];

                if (controller.Logger is FacilityPersonsCountLogger countLogger1)
                {
                    var data1 = countLogger1.GetDataForFacility("B_0");

                    Array.Resize(ref data, Math.Max(data.Length, data1.Max(x=>x.Item1)+1));

                    foreach (var pair in data1)
                    {
                        data[pair.Item1] += pair.Item2;
                    }

                }

                Controller.Random = new Random(0);
                Generate();
                Controller.Random = new Random(0);
                controller.Run(10000);

                if (controller.Logger is FacilityPersonsCountLogger countLogger2)
                {
                    var data1 = countLogger2.GetDataForFacility("B_0");

                    Array.Resize(ref data, Math.Max(data.Length, data1.Max(x => x.Item1)+1));

                    foreach (var pair in data1)
                    {
                        data[pair.Item1] -= pair.Item2;
                    }
                }

                Debug.WriteLine("Max delta: " + data.Max());

                this.Invoke(new Action(() =>
                {

                    new PlotForm(Enumerable.Range(0, data.Length).Select(x=>(x, data[x])).ToList()).Show();

                }));

            });
         
        }
        
        private void StartSimulation()
        {
            Task.Run(() =>
            {
                controller.RunAsync();

                // if (sessionId.HasValue)
                // {
                //     if (Controller.Logger is DBLogger logger1)
                //     {
                //         DrawPlot1(sessionId, logger1);
                //     }
                // }

                if (controller.Logger is FacilityPersonsCountLogger logger2)
                {

                    this.Invoke(new Action(() =>
                    {
                        var countData = logger2.GetData().ToDictionary(x => x.Key, x => x.Value.ToList());

                        new PlotForm(countData).Show();

                    }));
                }
            });
        }

        private void DrawPlot1(int? sessionId, DBLogger logger)
        {
            var collection = logger.CreateConnection().GetCollection<PersonInFacilityTime>();
            // int count = collection.Query().Where(x=>x.SessionId == sessionId).Count();
            // List<PersonInFacilityTime> data = collection.Query().Where(x => x.SessionId == sessionId && x.Person == "p1").Limit(100).ToList();
            // File.WriteAllLines("last_session_data.txt", data.Select(x=>
            //     $"{new LogCityTime(x.StartDay, x.StartMin)} - {new LogCityTime(x.EndDay, x.EndMin)}: {x.Person} -> {x.Facility}"
            // ));
            // Debug.WriteLine("Log sample saved");

            string facilityName = "I1_1";
            var data = collection.Query()
                .Where(x => x.SessionId == sessionId.Value && x.Facility == facilityName)
                .ToList();

            List<(int, int)> personInFacilityTimes =
                data.Select(x =>
                    (x.StartDay * 24 * 60 + x.StartMin, x.EndDay * 24 * 60 + x.EndMin)).ToList();

            int maxTime = personInFacilityTimes.Max(x => x.Item2);

            int delta = 5;
            int halfDelta = delta / 2;

            ConcurrentBag<(int, int)> countData = new ConcurrentBag<(int, int)>();

            // OrderablePartitioner<Tuple<int, int>> rangePart = Partitioner.Create(0, maxTime);
            //
            // Parallel.ForEach(rangePart, (range, loopState) =>
            // {
            //     var personInFacilityTimesClone = personInFacilityTimes
            //         .Where(x => (x.Item1 + halfDelta) > range.Item1 && (x.Item2 - halfDelta) < range.Item2)
            //         .ToArray().ToList();
            //
            //     int prevCount = 0;
            //     for (int i = range.Item1; i < range.Item2; i += delta)
            //     {
            //         int count = 0;
            //         for (int j = 0; j < personInFacilityTimesClone.Count; j++)
            //         {
            //             if (personInFacilityTimesClone[j].Item1 - halfDelta < i &&
            //                 personInFacilityTimesClone[j].Item2 + halfDelta > i)
            //             {
            //                 count++;
            //             }
            //         }
            //         if (count != prevCount)
            //         {
            //             prevCount = count;
            //             countData.Add((i, count));
            //             personInFacilityTimesClone.RemoveAll(x => x.Item2 + halfDelta < i);
            //         }
            //     }
            //
            // });


            int prevCount = 0;
            for (int i = 0; i < maxTime; i += delta)
            {
                int count = 0;
                for (int j = 0; j < personInFacilityTimes.Count; j++)
                {
                    if (personInFacilityTimes[j].Item1 - halfDelta < i &&
                        personInFacilityTimes[j].Item2 + halfDelta > i)
                    {
                        count++;
                    }
                }


                if (count != prevCount)
                {
                    countData.Add((i - delta / 2, prevCount));
                    countData.Add((i, count));
                    prevCount = count;
                    personInFacilityTimes.RemoveAll(x => x.Item2 + halfDelta < i);
                }
            }

            List<(int, int)> sortedCountData = countData.OrderBy(x => x.Item1).ToList();
            this.Invoke(new Action(() =>
            {
                new PlotForm(new Dictionary<string, List<(int, int)>>()
                {
                    {"I1_1", sortedCountData}
                }).Show();
            }));
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            panel1.Invalidate();
            time_label.Text = Controller.CurrentTime.ToString();
            Debug.WriteLine("Инфицированно: " + Controller.Instance.City.Persons.Count(x => x.HealthData.Infected));

            if (Controller.IsRunning && !Controller.Paused)
            {
                foreach (var pair in plots)
                {
                    pair.Key.AddPoint(pair.Value());
                }
            }
        }

        Point drawPos = new Point(0, 0);
        private float scale = 0.3f;
        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            for (var i = 0; i < commonDataSelector.Count; i++)
            {
                e.Graphics.DrawString(commonDataSelector[i](), SystemFonts.DefaultFont, Brushes.White, new PointF(0, e.Graphics.ClipBounds.Height - 15*(commonDataSelector.Count - i)));
            }

            int dataSelector = Math.Clamp(comboBox1.SelectedIndex, 0, comboBox1.Items.Count - 1);

            if (dataSelector > 0)
            {
                _facilityPersons = controller.City.Persons.GroupBy(x => x.Location).ToImmutableDictionary(x => x.Key, x => x.AsEnumerable());
            }

            e.Graphics.TranslateTransform(drawPos.X, drawPos.Y);
            e.Graphics.ScaleTransform(scale, scale);

            for (int i = 0; i < 100; i++)
            {
                e.Graphics.DrawString(i + " km", Renderer.DefaultFont, Brushes.White, i*1000, -20);
            }

            City city = Controller.Instance.City;

            foreach (Facility facility in city.Facilities.Values)
            {
                renderers[facility.GetType()].Render(facility, e.Graphics, facilitiesDataSelector[dataSelector], facilitiesColorSelector[dataSelector]);
            }

            personsRenderer.Render(city.Persons, e.Graphics);

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
                // TestSimulation();
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

        private void generateBtn_Click(object sender, EventArgs e)
        {
            // Controller.Random = new Random(0);
            Generate();
        }

        private Point? lastPos = null;
        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (lastPos.HasValue)
            {
                drawPos.X += e.X - lastPos.Value.X;
                drawPos.Y += e.Y - lastPos.Value.Y;
                lastPos = e.Location;
            }
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            lastPos = e.Location;
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            lastPos = null;
        }
        private void Panel1OnMouseWheel(object sender, MouseEventArgs e)
        {
            scale = Math.Max(0.01f, scale + e.Delta / 10000f);
        }

        private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem == InfectionSpred_toolStripMenuItem1)
            {
                OpenPlotFor(e.ClickedItem.Text, () => (Controller.CurrentTime.TotalMinutes, controller.City.Persons.Count(x => x.HealthData.HealthStatus == HealthStatus.InfectedSpread)));
            }

            if (e.ClickedItem == InfectionIncubate_toolStripMenuItem1)
            {
                OpenPlotFor(e.ClickedItem.Text, () => (Controller.CurrentTime.TotalMinutes, controller.City.Persons.Count(x => x.HealthData.HealthStatus == HealthStatus.InfectedIncubation)));
            }

            if (e.ClickedItem == Immune_toolStripMenuItem)
            {
                OpenPlotFor(e.ClickedItem.Text, () => (Controller.CurrentTime.TotalMinutes, controller.City.Persons.Count(x => x.HealthData.HealthStatus == HealthStatus.Immune)));
            }
        }
    }
}
