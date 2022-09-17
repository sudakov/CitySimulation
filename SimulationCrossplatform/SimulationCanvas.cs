using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CitySimulation.Entities;
using CitySimulation.Ver1.Entity;
using CitySimulation;
using CitySimulation.Ver2.Entity;
using SimulationCrossplatform.Render;
using CitySimulation.Health;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using CitySimulation.Behaviour.Action;
using CitySimulation.Tools;
using Point = Avalonia.Point;

namespace SimulationCrossplatform
{
    public class SimulationCanvas : Control
    {
        public static readonly SolidColorBrush TextBrush = new SolidColorBrush(Colors.Black);

        private Controller controller;

        private Dictionary<string, Renderer> renderers = new()
        {
            {"default", new FacilityRenderer(){Brush = Brushes.Black, TextBrush = TextBrush} },
            {"bus", new BusRenderer(){ Brush = Brushes.Blue, WaitingBrush = Brushes.Aqua, TextBrush = TextBrush} },
        };

        private PersonsRenderer _personsRenderer = new ();
        private RoutesRenderer _routeRenderer = new ();
        private TileRenderer _tileRenderer;

        private List<Func<Facility, string>> facilitiesDataSelector;
        private List<Func<Facility, IBrush>> facilitiesColorSelector;
        private List<Func<IEnumerable<Person>, IEnumerable<Person>>> personsSelector;

        private readonly HashSet<string> _visibleTypes = new ();

        private ImmutableDictionary<Facility, IEnumerable<Person>> facilityPersons;


        private Point _drawPos = new ();
        public Point DrawPoint
        {
            get => _drawPos;
            set => _drawPos = value;
        }

        private Point? _lastPos = null;
        private double _scale = 1f;

        public double TileOpacity { get; set; } = 1;

        public void Update(Controller controller)
        {
            this.controller = controller;
            InvalidateVisual();
        }

        public void Setup(TileRenderer tileRenderer)
        {
            this._tileRenderer = tileRenderer;
            tileRenderer.RunLoadTask(() => _drawPos, InvalidateVisual);
        }

        public void SetFacilityColors(Dictionary<string, string> facilityColors)
        {
            var pairs = facilityColors.ToList();

            foreach (var (facilityType, color) in pairs)
            {
                if (renderers.ContainsKey(facilityType))
                {
                    renderers.Remove(facilityType);
                }

                if (!Color.TryParse(color, out var parsedColor))
                {
                    parsedColor = uint.TryParse(color, out uint colorCode) ? Color.FromUInt32(colorCode) : Colors.Black;
                }


                renderers.Add(facilityType, new FacilityRenderer()
                {
                    Brush = new SolidColorBrush(parsedColor),
                    TextBrush = TextBrush
                });
            }
        }

        public void SetVisibility(string type, bool visible)
        {
            if (visible)
            {
                _visibleTypes.Add(type);
            }
            else
            {
                _visibleTypes.Remove(type);
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            ClipToBounds = true;

            facilitiesDataSelector = new List<Func<Facility, string>>()
            {
                facility => facility.PersonsCount.ToString(),
                facility => (facilityPersons.GetValueOrDefault(facility, null)?.Count(x => x.Age < 18) ?? 0).ToString(),
                facility => (facilityPersons.GetValueOrDefault(facility, null)?.Count(x=>x.Age >= 60) ?? 0).ToString(),
                facility =>
                {
                    int spread = facilityPersons.GetValueOrDefault(facility, null)?.Count(x=>x.HealthData.HealthStatus == HealthStatus.InfectedSpread) ?? 0;
                    int incub = facilityPersons.GetValueOrDefault(facility, null)?.Count(x=>x.HealthData.HealthStatus == HealthStatus.InfectedIncubation) ?? 0;
                    return spread + "/" + incub;
                },
                facility => (facilityPersons.GetValueOrDefault(facility, null)?.Count(x=>x.Behaviour?.CurrentAppointment != null) ?? 0).ToString()
            };

            facilitiesColorSelector = new List<Func<Facility, IBrush>>()
            {
                null,
                null,
                null,
                facility =>
                {
                    bool spread = facilityPersons.GetValueOrDefault(facility, null)?.Any(x => x.HealthData.HealthStatus == HealthStatus.InfectedSpread) == true;
                    bool incub = facilityPersons.GetValueOrDefault(facility, null)?.Any(x => x.HealthData.HealthStatus == HealthStatus.InfectedIncubation) == true;
                    return new SolidColorBrush(Color.FromArgb(255, (byte)(spread || incub ? 255 : 0), (byte)(incub && !spread ? 255 : 0), 0)) as IBrush;
                },
                facility => (facility is Service service && !(facility is School) ? (facilityPersons.GetValueOrDefault(facility, null)?.Count(x=>x.CurrentAction is ServiceVisiting) > 0 ? Brushes.LawnGreen : Brushes.DarkGreen) : null),
            };

            personsSelector = new List<Func<IEnumerable<Person>, IEnumerable<Person>>>()
            {
                null,
                null,
                null,
                null,
                null,
            };
        }


        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_lastPos.HasValue)
            {
                _drawPos = new Point(
                    _drawPos.X + (e.GetPosition(null).X - _lastPos.Value.X) / _scale,
                    _drawPos.Y - (e.GetPosition(null).Y - _lastPos.Value.Y) / _scale);
                
                _lastPos = e.GetPosition(null);
                InvalidateVisual();
            }
            base.OnPointerMoved(e);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            _lastPos = e.GetPosition(null);
            base.OnPointerPressed(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            _lastPos = null;
            base.OnPointerReleased(e);
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            var deltaY = e.Delta.Y / 10f;

            if (_scale < 0.1f)
            {
                deltaY /= 4;
            }

            _scale = Math.Max(0.01f, _scale + deltaY);
            // _drawPos += new Point(_drawPos.X * deltaY, _drawPos.Y * deltaY);

            InvalidateVisual();

            base.OnPointerWheelChanged(e);
        }

        public override void Render(DrawingContext context)
        {
            context.FillRectangle(Brushes.Black, Bounds);

            if (controller == null)
            {
                return;
            }

            byte colorVal = (byte)(255 * (1 - TileOpacity));
            TextBrush.Color = Color.FromArgb(255, colorVal, colorVal, colorVal);

            using (context.PushSetTransform((Matrix.CreateTranslation(_drawPos.X, -_drawPos.Y))))
            {
                using (context.PushPostTransform(Matrix.CreateScale(_scale, _scale)))
                {
                    using (context.PushPostTransform(Matrix.CreateTranslation(Bounds.Width/2, Bounds.Height / 2)))
                    {
                        if (TileOpacity > 0 && _visibleTypes.Contains("tiles"))
                        {
                            using (context.PushOpacity(TileOpacity))
                            {
                                _tileRenderer.Render(context, -_drawPos.ScreenToMap(), InvalidateVisual, _scale);
                            }
                        }

                        facilityPersons = controller.City.Persons.GroupBy(x => x.Location).Where(x => x.Key != null).ToImmutableDictionary(x => x.Key, x => x.AsEnumerable());

                        int dataSelector = 0;

                        City city = Controller.Instance.City;

                        if (_visibleTypes.Contains("route"))
                        {
                            _routeRenderer.Render(city.Routes, context);
                        }

                        var lookup = city.Facilities.Values.ToLookup(x=>x is Transport);

                        foreach (var facilities in new[] { lookup[false], lookup[true]})
                        {
                            foreach (Facility facility in facilities)
                            {
                                if (_visibleTypes.Contains(facility.Type))
                                {
                                    var renderer = renderers.GetValueOrDefault(facility.Type, renderers["default"]);
                                    renderer?.Render(facility, context, facilitiesDataSelector[dataSelector], facilitiesColorSelector[dataSelector]);
                                }
                            }
                        }

                        {
                            var personsToRender = personsSelector[dataSelector] == null ? city.Persons : personsSelector[dataSelector](city.Persons);
                            if (!_visibleTypes.Contains("[people in transport]"))
                            {
                                personsToRender = personsToRender.Where(x => x.Location is not Transport);
                            }

                            _personsRenderer.Render(personsToRender, context);
                        }


                        foreach (var facilities in new[] { lookup[false], lookup[true] })
                        {
                            foreach (Facility facility in facilities)
                            {
                                if (_visibleTypes.Contains(facility.Type))
                                {
                                    var renderer = renderers.GetValueOrDefault(facility.Type, renderers["default"]);
                                    renderer?.RenderText(facility, context, facilitiesDataSelector[dataSelector], facilitiesColorSelector[dataSelector]);
                                }
                            }
                        }
                    }
                }
            }
        }


    }
}
