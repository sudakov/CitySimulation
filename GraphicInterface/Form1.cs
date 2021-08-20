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
using CitySimulation.Behaviour.Action;
using CitySimulation.Control;
using CitySimulation.Control.Log;
using CitySimulation.Control.Log.DbModel;
using CitySimulation.Generation;
using CitySimulation.Generation.Areas;
using CitySimulation.Generation.Models;
using CitySimulation.Generation.Persons;
using CitySimulation.Tools;
using GraphicInterface.Render;
using Module = CitySimulation.Control.Module;
using Point = System.Drawing.Point;
using Range = CitySimulation.Tools.Range;
using System.Xml.Serialization;
using CitySimulation.Control.Modules;
using CitySimulation.Entities;
using CitySimulation.Xml;
using CitySimulation.Generation.Model2;
using CitySimulation.Health;
using CitySimulation.Ver1.Entity;
using CitySimulation.Ver2.Entity;
using CitySimulation.Ver2.Generation;

namespace GraphicInterface
{
    public partial class Form1 : Form
    {
        private Controller controller;

        private Dictionary<Type, Renderer> renderers = new Dictionary<Type, Renderer>()
        {
            {typeof(FacilityConfigurable), new FacilityRenderer(){Brush = Brushes.Yellow} },
            {typeof(Station), new FacilityRenderer(){Brush = Brushes.Red} },
            {typeof(Office), new FacilityRenderer(){Brush = Brushes.Blue} },
            {typeof(LivingHouse), new FacilityRenderer(){Brush = Brushes.Yellow} },
            {typeof(Service), new FacilityRenderer(){Brush = Brushes.LawnGreen} },
            {typeof(RecreationService), new FacilityRenderer(){Brush = Brushes.LawnGreen} },
            {typeof(School), new FacilityRenderer(){Brush = Brushes.DarkSlateGray} },
            {typeof(Bus), new BusRenderer(){Brush = Brushes.Cyan, WaitingBrush = Brushes.DarkCyan} }
        };

        private PersonsRenderer personsRenderer = new PersonsRenderer();

        #region DataSelectors

        

        private List<Func<string>> commonDataSelector;


        private List<Func<Facility, string>> facilitiesDataSelector;
        private List<Func<Facility, Brush>> facilitiesColorSelector;
        private List<Func<IEnumerable<Person>, IEnumerable<Person>>> personsSelector;
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

            comboBox1.Items.AddRange(new object[]
            {
                "Кол-во людей",
                "Кол-во детей",
                "Кол-во пожилых",
                "Заражённость",
                "Кол-во посетителей",
            });

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
                },
                facility => (FacilityPersons.GetValueOrDefault(facility, null)?.Count(x=>x.Behaviour?.CurrentAppointment != null) ?? 0).ToString()
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
                facility => (facility is Service service && !(facility is School) ? (FacilityPersons.GetValueOrDefault(facility, null)?.Count(x=>x.CurrentAction is ServiceVisiting) > 0 ? Brushes.LawnGreen : Brushes.DarkGreen) : null),
            };

            personsSelector = new List<Func<IEnumerable<Person>, IEnumerable<Person>>>()
            {
                null,
                null,
                null,
                null,
                null,
            };


            comboBox1.SelectedIndex = 0;


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


            //--------------------------


            Generate2();

            return controller.City;
        }

        private void Generate1()
        {
            controller = new ControllerComplex()
            {
                VirusSpreadModule = new VirusSpreadModule(),
                Context = new Context()
                {
                    Logger = new FacilityPersonsCountLogger(),
                    Random = Controller.Random,
                },
            };

            controller.Modules.AddRange(new List<Module>() { new ServiceAppointmentModule(), controller.Context.Logger, controller.VirusSpreadModule }.Where(x => x != null).ToList());


            House h9 = new House()
            {
                Name = "Девятиэтажный",
                Size = (int) Math.Ceiling(Math.Sqrt(0.4 * 10000)),
                FamiliesPerHouse = 140
            };
            House h5 = new House()
            {
                Name = "Пятиэтажный",
                Size = (int) Math.Ceiling(Math.Sqrt(0.3 * 10000)),
                FamiliesPerHouse = 80
            };
            House h3 = new House()
            {
                Name = "Трёхэтажный",
                Size = (int) Math.Ceiling(Math.Sqrt(0.3 * 10000)),
                FamiliesPerHouse = 24
            };

            House h1 = new House()
            {
                Name = "Частный",
                Size = (int) Math.Ceiling(Math.Sqrt(0.1 * 10000)),
                FamiliesPerHouse = 1
            };

            AgesConfig agesConfig = new AgesConfig()
            {
                AdultAge = new Range(18, 65),
                WorkerAgeRange = new Range(20, 65),
                StudentAgeRange = new Range(2, 20),
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
                AgeConfig = agesConfig,
            };

            Model1 model = new Model1()
            {
                AgesConfig = agesConfig,
                OnFootDistance = 500,
                DistanceBetweenStations = 500,
                AreaSpace = 200,
                Areas = new Area[]
                {
                    new IndustrialArea()
                    {
                        Name = "I1",
                        WorkplacesRatio = 0.175f,
                        HouseSize = 100,
                        HouseSpace = 20,
                        AreaLength = 1000,
                        Offices = new[]
                        {
                            new IndustrialArea.OfficeConfig
                            {
                                WorkersCount = 800,
                                WorkTime = (8 * 60, 17 * 60)
                            },
                            new IndustrialArea.OfficeConfig
                            {
                                WorkersCount = 1200,
                                WorkTime = (9 * 60, 18 * 60)
                            },
                        }
                    },
                    new AdministrativeArea()
                    {
                        Name = "Adm",
                        AreaDepth = 600,
                        HouseSpace = 100,
                        Service = new[]
                        {
                            new AdministrativeService("МФЦ")
                            {
                                Size = new CitySimulation.Tools.Point(100, 100),
                                WorkersCount = 60,
                                MaxWorkersCount = 60,
                                WorkTime = new Range(9 * 60, 16 * 60),
                                ForceAppointment = true,
                                VisitDuration = 60,
                            },
                            new AdministrativeService("ПРФ")
                            {
                                Size = new CitySimulation.Tools.Point(100, 100),
                                WorkTime = new Range(10 * 60, 16 * 60),
                                WorkersCount = 40,
                                MaxWorkersCount = 40,
                                ForceAppointment = true,
                                VisitDuration = 60
                            },
                            new AdministrativeService("ФНС")
                            {
                                Size = new CitySimulation.Tools.Point(100, 100),
                                WorkTime = new Range(9 * 60, 16 * 60),
                                WorkersCount = 50,
                                MaxWorkersCount = 50,
                                ForceAppointment = true,
                                VisitDuration = 60,
                            },
                            new AdministrativeService("ФСС")
                            {
                                Size = new CitySimulation.Tools.Point(100, 100),
                                WorkTime = new Range(10 * 60, 16 * 60),
                                WorkersCount = 30,
                                MaxWorkersCount = 30,
                                ForceAppointment = true,
                                VisitDuration = 60,
                            },
                            new AdministrativeService("Военкомат")
                            {
                                Size = new CitySimulation.Tools.Point(100, 100),
                                WorkTime = new Range(9 * 60, 16 * 60),
                                WorkersCount = 20,
                                MaxWorkersCount = 20,
                                ForceAppointment = true,
                                VisitDuration = 60,
                            },
                        }
                    },
                    new ResidentialArea()
                    {
                        Name = "Old",
                        HouseSpace = 5,
                        Houses = new[] {h3, h5},
                        AreaDepth = (int) (0.6 * 1000),
                        HousesDistribution = new[] {0.5, 0.5},
                        SchoolDistance = (300, 600),
                        FamiliesPerSchool = 892,
                    },
                    new ResidentialArea()
                    {
                        Name = "Private",
                        HouseSpace = 5,
                        Houses = new[] {h1},
                        AreaDepth = (int) (0.6 * 1000),
                        SchoolDistance = (300, 600),
                        FamiliesPerSchool = 892,
                    },
                    new ResidentialArea()
                    {
                        Name = "New",
                        HouseSpace = 5,
                        Houses = new[] {h9},
                        AreaDepth = (int) (0.6 * 1000),
                        SchoolDistance = (300, 600),
                        FamiliesPerSchool = 892,
                    },
                    new IndustrialArea()
                    {
                        Name = "I2",
                        WorkplacesRatio = 0.175f,
                        HouseSize = 100,
                        HouseSpace = 20,
                        AreaLength = 1000,
                        Offices = new[]
                        {
                            new IndustrialArea.OfficeConfig
                            {
                                WorkersCount = 1000,
                                WorkTime = (10 * 60, 19 * 60)
                            },
                            new IndustrialArea.OfficeConfig
                            {
                                WorkersCount = 2000,
                                WorkTime = (8 * 60 + 30, 17 * 60 + 30)
                            },
                        }
                    },
                },

                ServicesConfig = new ServicesConfig
                {
                    ServiceWorkersRatio = 0.4f,
                    LocalWorkersRatio = 0.41f,
                    ServicesData = new List<ServicesConfig.ServiceDataBase>()
                    {
                        new ServicesConfig.ServiceData<HouseholdService>("парикмахерская", "ремонт")
                        {
                            Prefix = "Прочее",
                            WorkTime = new Range(8 * 60, 17 * 60),
                            WorktimeRandoms = new[] {-60, -30, 0, 30, 60},
                            WorkersPerService = new Range(1, 15),
                            FamiliesPerService = 250,
                            Salary = (25000, 30000),
                            Overheads = (3, 5),
                            ServiceCost = (300, 5000),
                        },
                        new ServicesConfig.ServiceData<Store>("маркет")
                        {
                            Prefix = "Магазин",
                            WorkTime = new Range(8 * 60, 21 * 60),
                            WorktimeRandoms = new[] {-60, -30, 0, 30, 60},
                            WorkersPerService = new Range(15, 30),
                            FamiliesPerService = 1000,
                            Salary = (25000, 30000),
                            Overheads = (3, 5),
                            ServiceCost = (300, 5000),
                        },
                        new ServicesConfig.ServiceData<RecreationService>("вечерние курсы", "клуб", "бассейн",
                            "спортзал", "стадион", "ресторан", "пивбар")
                        {
                            Prefix = "Отдых",
                            WorkTime = new Range(8 * 60, 17 * 60),
                            WorktimeRandoms = new[] {-60, -30, 0, 30, 60},
                            WorkersPerService = new Range(5, 25),
                            FamiliesPerService = 10000 / 3,
                            Salary = (25000, 30000),
                            Overheads = (3, 5),
                            ServiceCost = (300, 5000),
                        }
                    },
                },
                BusesSpeedAndCapacities = new (int, int)[]
                {
                    (300, 100),
                    (300, 100),
                    (300, 100),
                    (300, 100),
                    (300, 100),
                    // (500, 100),
                    // (500, 100),
                    // (500, 100),
                    // (500, 100),
                    // (500, 100),

                    // (500, 100),
                    // (500, 100),
                    // (500, 100),
                    (500, 30),
                    (500, 30),
                    (500, 30),
                },
            };

            // model.ServicesConfig.ServicesData.Add(model.ServicesConfig.ServicesData[0]);
            //
            // var stream = new FileStream("test.xml", FileMode.Create);
            // var xmlConfigManager = new XmlConfigManager();
            // xmlConfigManager.AddConverter(new RangeXmlConverter());
            // xmlConfigManager.AddConverter(new ServiceXmlConverter());
            // xmlConfigManager.AddConverter(new PointXmlConverter());
            // xmlConfigManager.WriteObject(model, stream);
            // stream.Close();
            // model = xmlConfigManager.ReadObject<Model1>(new FileStream("test.xml", FileMode.Open));

            PersonBehaviourGenerator behaviourGenerator = new PersonBehaviourGenerator()
            {
                AgesConfig = agesConfig
            };

            //Генерируем население
            List<Person> persons = personsGenerator.Generate();
            List<Family> families = persons.Select(x => x.Family).Distinct().ToList();

            //Настраиванием поведение
            persons.ForEach(x => behaviourGenerator.GenerateBehaviour(x));


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

            controller.DeltaTime = (int) numericUpDown1.Value;
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
                    AgesCount = new[]
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
        }

        private void Generate2()
        {
            controller = new ControllerSimple()
            {
                Context = new Context()
                {
                    Logger = new FacilityPersonsCountLogger(),
                    Random = Controller.Random,
                },
                Modules = new List<Module>()
                {
                    new TraceModule()
                    {
                        Filename = "output.csv"
                    }
                }
            };

            Model2 model = new Model2()
            {
                FileName = "UPDESUA.json"
            };

            RunConfig config = model.Configuration();

            controller.Context.Random = new Random(config.Seed);

            controller.City = model.Generate(controller.Context.Random);
            controller.DeltaTime = (int)numericUpDown1.Value;
            controller.Setup();
            foreach (var person in controller.City.Persons.Take(5))
            {
                person.HealthData.TryInfect();
            }
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

                if (controller.Context.Logger is FacilityPersonsCountLogger countLogger1)
                {
                    var data1 = countLogger1.GetDataForFacility("Bus_0");

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

                if (controller.Context.Logger is FacilityPersonsCountLogger countLogger2)
                {
                    var data1 = countLogger2.GetDataForFacility("Bus_0");

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
                controller.RunAsync(3);

                if (controller.Context.Logger is FacilityPersonsCountLogger logger2)
                {

                    // this.Invoke(new Action(() =>
                    // {
                    //     var countData = logger2.GetData().ToDictionary(x => x.Key, x => x.Value.ToList());
                    //
                    //     new PlotForm(countData).Show();
                    //
                    // }));

                    this.Invoke(new Action(() =>
                    {
                        var countData = logger2.GetVisitorsData().ToDictionary(x => x.Key, x => x.Value.ToList());

                        new PlotForm(countData, PlotForm.PlotType.Gistogram).Show();

                    }));
                }
            });
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            _facilityPersons = controller.City.Persons.GroupBy(x => x.Location).ToImmutableDictionary(x => x.Key, x => x.AsEnumerable());

            panel1.Invalidate();
            time_label.Text = controller.Context.CurrentTime.ToString();
            // Debug.WriteLine("Инфицированно: " + Controller.Instance.City.Persons.Count(x => x.HealthData.Infected));

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

            
            e.Graphics.TranslateTransform(drawPos.X, drawPos.Y);
            e.Graphics.ScaleTransform(scale, scale);

            for (int i = 0; i < 100; i++)
            {
                e.Graphics.DrawString(i + " km", Renderer.DefaultFont, Brushes.White, i*1000, -20);
            }

            City city = Controller.Instance.City;

            foreach (Facility facility in city.Facilities.Values)
            {
                var renderer = renderers.GetValueOrDefault(facility.GetType(), facility is Service ? renderers[typeof(Service)] : null);
                renderer?.Render(facility, e.Graphics, facilitiesDataSelector[dataSelector], facilitiesColorSelector[dataSelector]);
            }

            personsRenderer.Render(personsSelector[dataSelector] == null ? city.Persons : personsSelector[dataSelector](city.Persons), e.Graphics);

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

            stop_minutes = int.Parse(split[0]) * 60 + int.Parse(split[1]) - controller.Context.CurrentTime.Minutes;
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

            int Distance(Facility facility)
            {
                if (facility.Coords == null)
                {
                    return int.MaxValue;
                }
                int x = (int)((e.X - drawPos.X) / scale) - facility.Coords.X - (facility.Size?.X ?? Renderer.DefaultSize.X) / 2;
                int y = (int)((e.Y - drawPos.Y) / scale) - facility.Coords.Y - (facility.Size?.Y ?? Renderer.DefaultSize.Y) / 2;
                // Debug.WriteLine(x + " " + y);
                return x * x + y * y;
            }

            Facility closest = controller.City.Facilities.Values.MinBy(Distance);

            // if (Distance(closest) < (Renderer.DefaultSize.X / 2) * (Renderer.DefaultSize.X / 2))
            // {
            //     Debug.WriteLine(closest.Name);
            // }

            // Facility facility = controller.City.Facilities["St_top0"];  
            // {
            //    
            // }
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
                OpenPlotFor(e.ClickedItem.Text, () => (controller.Context.CurrentTime.TotalMinutes, controller.City.Persons.Count(x => x.HealthData.HealthStatus == HealthStatus.InfectedSpread)));
            }

            if (e.ClickedItem == InfectionIncubate_toolStripMenuItem1)
            {
                OpenPlotFor(e.ClickedItem.Text, () => (controller.Context.CurrentTime.TotalMinutes, controller.City.Persons.Count(x => x.HealthData.HealthStatus == HealthStatus.InfectedIncubation)));
            }

            if (e.ClickedItem == Immune_toolStripMenuItem)
            {
                OpenPlotFor(e.ClickedItem.Text, () => (controller.Context.CurrentTime.TotalMinutes, controller.City.Persons.Count(x => x.HealthData.HealthStatus == HealthStatus.Immune)));
            }

            if (e.ClickedItem == Visitors_toolStripMenuItem)
            {
                var listBox = new ListBox()
                {
                    DataSource = controller.City.Facilities.Values.OfType<Service>().OrderBy(x=>x.Name).ToList(),
                    DisplayMember = "NameMember",
                    Dock = DockStyle.Fill,
                };

                Form form = new Form()
                {
                    Controls = {listBox}
                };


                listBox.DoubleClick += (o, args) =>
                {
                    var item = listBox.SelectedItem;
                    if (item is Service service)
                    {
                        OpenPlotFor("Посетители: " + service.Name, () =>
                            {
                                Service s = service;
                                return (
                                    controller.Context.CurrentTime.TotalMinutes,
                                    FacilityPersons.GetValueOrDefault(s, null)
                                        ?.Count(x => x.CurrentAction is ServiceVisiting) ?? 0
                                );
                            }
                        );

                        form.Close();
                    }
                };
                form.Show();
            }

            if (e.ClickedItem == Persons_toolStripMenuItem)
            {
                var listBox = new ListBox()
                {
                    DataSource = controller.City.Facilities.Values.OrderBy(x=>x.Name).ToList(),
                    DisplayMember = "NameMember",
                    Dock = DockStyle.Fill,
                };

                Form form = new Form()
                {
                    Controls = { listBox }
                };


                listBox.DoubleClick += (o, args) =>
                {
                    var item = listBox.SelectedItem;
                    if (item is Facility facility)
                    {
                        OpenPlotFor("Кол-во людей: " + facility.Name, () =>
                            {
                                Facility f = facility;
                                return (
                                    controller.Context.CurrentTime.TotalMinutes,
                                    FacilityPersons.GetValueOrDefault(f, null)
                                        ?.Count() ?? 0
                                );
                            }
                        );

                        form.Close();
                    }
                };
                form.Show();
            }
        }

    }
}
