using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CitySimulation.Entities;
using CitySimulation.Health;
using CitySimulation.Tools;
using SimulationCrossplatform.Utils;
using Point = Avalonia.Point;
using SimPoint = CitySimulation.Tools.Point;

namespace SimulationCrossplatform.Render
{
    public class PersonsRenderer
    {
        private class PersonsGroup
        {
            public int Count = 0;
            public SimPoint CoordSum = SimPoint.Zero;
            public int InfectedCount = 0;
            public int ImmuneCount = 0;

            public SimPoint Coord => CoordSum / Count;
            public double InfectedRatio => InfectedCount / (double)Count;
            public double ImmuneRatio => ImmuneCount / (double)Count;

            public override string ToString()
            {
                return $"{Count - InfectedCount - ImmuneCount}|{InfectedCount}|{ImmuneCount}";
            }
        }

        private const int GROUP_DISTANCE = 100;
        private const int BUBBLE_BASE_SIZE = 10;

        public IBrush TextBrush = Brushes.Black;

        public Color UninfectedColor
        {
            get => _uninfectedColor.ToRgbColor();
            set => _uninfectedColor = new HsvColor(value);
        }


        private HsvColor _uninfectedColor = new HsvColor(Colors.LawnGreen);
        public Color InfectedColor 
        {
            get => _infectedColor.ToRgbColor();
            set => _infectedColor = new HsvColor(value);
        }
        private HsvColor _infectedColor = new HsvColor(Colors.Red);
        public Color ImmuneColor
        {
            get => _immuneColor.ToRgbColor();
            set => _immuneColor = new HsvColor(value);
        }
        private HsvColor _immuneColor = new HsvColor(Colors.LightBlue);

        private readonly Dictionary<SimPoint, PersonsGroup> _dictionary = new();

        public void Render(IEnumerable<Person> persons, DrawingContext g)
        {
            _dictionary.Clear();

            foreach (var person in persons)
            {
                var coords = person.CalcCoords();
                if (coords != null)
                {
                    var group = _dictionary.GetOrSetDefault(coords.Value / GROUP_DISTANCE, new PersonsGroup());
                    group.Count++;
                    group.CoordSum += coords.Value;

                    switch (person.HealthData.HealthStatus)
                    {
                        case HealthStatus.InfectedIncubation or HealthStatus.InfectedSpread:
                            group.InfectedCount++;
                            break;
                        case HealthStatus.Recovered:
                            group.ImmuneCount++;
                            break;
                    }
                }
            }

            foreach (var group in _dictionary.Values)
            {
                var v = (int)(Math.Sqrt(group.Count) * BUBBLE_BASE_SIZE);
                var coord = group.Coord;
                double infectedRatio = group.InfectedRatio;
                double immuneRatio = group.ImmuneRatio;

                var color = new HsvColor(
                    _uninfectedColor.Hue * (1 - infectedRatio - immuneRatio) + _infectedColor.Hue * infectedRatio + _immuneColor.Hue * immuneRatio,
                    _uninfectedColor.Saturation,
                    _uninfectedColor.Value
                    );

                var brush = new SolidColorBrush(color.ToRgbColor());

                g.DrawEllipse(brush, new Pen(brush), new Point(coord.X - v / 2, -coord.Y - v / 2).MapToScreen(), v, v);
                g.DrawText(TextBrush, new Point(coord.X - v, -coord.Y - v/2 - 10).MapToScreen(), 
                    new FormattedText(group.Count > 5 ? group.ToString() : group.Count.ToString(), Typeface.Default, 20, TextAlignment.Center, TextWrapping.NoWrap, new Size(v,v)));
            }
        }
    }
}
