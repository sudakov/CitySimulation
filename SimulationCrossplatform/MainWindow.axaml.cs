using Avalonia.Controls;
using CitySimulation;
using CitySimulation.Control;
using CitySimulation.Ver2.Control;
using CitySimulation.Ver2.Generation;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CitySimulation.Ver2.Entity;
using CitySimulation.Ver2.Generation.Osm;
using SimulationCrossplatform.Render;

namespace SimulationCrossplatform
{
    public partial class MainWindow : Window
    {
        private Controller controller;
        private int numThreads;
        private DateTime _lastTime = DateTime.Now;

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow Setup(string configPath)
        {
            controller = GenerateOsm(configPath);

            controller.OnLifecycleFinished += Controller_OnLifecycleFinished;

            void VisibilityCheckBoxOnValueChanged(object? sender, RoutedEventArgs e)
            {
                CheckBox c = (CheckBox)sender;
                SimulationCanvas.SetVisibility(c.Content?.ToString(), c.IsChecked == true);
                SimulationCanvas.InvalidateVisual();
            }

            foreach (var facilityType in controller.City.Facilities.Values.Select(x=>x.Type).Distinct())
            {
                CheckBox checkBox = new CheckBox()
                {
                    Content = facilityType,
                    IsChecked = true
                };

                checkBox.Unchecked += VisibilityCheckBoxOnValueChanged;
                checkBox.Checked += VisibilityCheckBoxOnValueChanged;

                VisibilityPanel.Children.Add(checkBox);
                SimulationCanvas.SetVisibility(facilityType, true);
            }

            //People bubble in transport checkbox
            {
                CheckBox checkBox = new CheckBox()
                {
                    Content = "[people in transport]",
                    IsChecked = true
                };

                checkBox.Unchecked += VisibilityCheckBoxOnValueChanged;
                checkBox.Checked += VisibilityCheckBoxOnValueChanged;

                VisibilityPanel.Children.Add(checkBox);
                SimulationCanvas.SetVisibility("[people in transport]", true);
            }

            //routes checkbox
            {
                CheckBox checkBox = new CheckBox()
                {
                    Content = "route",
                    IsChecked = true
                };

                checkBox.Unchecked += VisibilityCheckBoxOnValueChanged;
                checkBox.Checked += VisibilityCheckBoxOnValueChanged;

                VisibilityPanel.Children.Add(checkBox);
                SimulationCanvas.SetVisibility("route", true);
            }

            double aX = controller.City.Facilities.Values.OfType<FacilityConfigurable>().Select(x=>x.Coords.X).Average();
            double aY = controller.City.Facilities.Values.OfType<FacilityConfigurable>().Select(x=>x.Coords.Y).Average();
            SimulationCanvas.DrawPoint = new Avalonia.Point(-aX, -aY).MapToScreen();

            SimulationCanvas.Update(controller);

            return this;
        }



        private Controller GenerateSimple()
        {
            ModelSimple model = new ModelSimple()
            {
                FileName = "UPDESUA.json",
                UseTransport = true
            };

            RunConfig config = model.Configuration();

            Random random = new Random(config.Seed);

            City city = model.Generate(random);


            var controller = new ControllerSimple()
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

            numThreads = config.NumThreads;

            //Запуск симуляции
            // controller.RunAsync(config.NumThreads);

            return controller;
        }

        private Controller GenerateOsm(string configPath)
        {
            var model = new OsmModel()
            {
                FileName = configPath,
                UseTransport = true
            };


            var facilityColors = model.FacilityColors();
            SimulationCanvas.SetFacilityColors(facilityColors);

            RunConfig config = model.Configuration();

            Random random = new Random(config.Seed);

            City city = model.Generate(random);


            var controller = new ControllerSimple()
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

            controller.Setup();

            controller.OnLifecycleFinished += () =>
            {
                if (controller.Context.CurrentTime.Day >= config.DurationDays)
                {
                    Console.WriteLine("---------------------");
                    Controller.IsRunning = false;
                }
            };

            numThreads = config.NumThreads;

            return controller;
        }

        private void StartSimulation()
        {
            Task.Run(() =>
            {
                controller.RunAsync(numThreads);
            });
        }

        private void Controller_OnLifecycleFinished()
        {
            if (DateTime.Now - _lastTime > TimeSpan.FromMilliseconds(10))
            {
                _lastTime = DateTime.Now;
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    TimeLabel.Text = controller.Context.CurrentTime.ToString();
                    SimulationCanvas.Update(controller);
                });
            }
            // if (_lifeStep++ % 100 == 0)
            // {
            //     Dispatcher.UIThread.InvokeAsync(() =>
            //     {
            //         TimeLabel.Text = controller.Context.CurrentTime.ToString();
            //         SimulationCanvas.Update(controller);
            //     });
            // }
        }

        private void StartButton_OnClick(object? sender, RoutedEventArgs e)
        {
            Controller.Paused = false;
            if (!Controller.IsRunning)
            {
                StartSimulation();
            }
        }

        private void PauseButton_OnClick(object? sender, RoutedEventArgs e)
        {
            Controller.Paused = true;
        }

        private void StopButton_OnClick(object? sender, RoutedEventArgs e)
        {
            Controller.IsRunning = false;
            controller.Setup();
        }

        private void SleepTime_OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            controller.SleepTime = (int)e.NewValue;
        }

        private void DeltaTime_OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            controller.DeltaTime = (int)e.NewValue;
        }

        private void TilesOpacitySlider_OnPointerMoved(object? sender, PointerEventArgs e)
        {
            SimulationCanvas.TileOpacity = TilesOpacitySlider.Value;
            SimulationCanvas.InvalidateVisual();
        }
    }
}
