using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CitySimulation.Entities;

namespace SimulationCrossplatform.Render
{
    public class PersonsRenderer
    {
        public IBrush TextBrush = Brushes.Black;
        public IBrush LinkBrush = Brushes.LawnGreen;
        private int delta = 50;
        private int size = 10;
        public void Render(IEnumerable<Person> persons, DrawingContext g)
        {
            var points = new Dictionary<CitySimulation.Tools.Point?, int>();

            foreach (var person in persons.Where(x=>x.Location is not Transport))
            {
                var coords = person.CalcCoords();
                if (coords != null)
                {
                    CitySimulation.Tools.Point? point = points.Keys.FirstOrDefault(x => CitySimulation.Tools.Point.Distance(coords.Value, x.Value) < delta);
                    if (point != null)
                    {
                        points[point] += 1;
                    }
                    else
                    {
                        points.Add(coords.Value, 1);
                    }
                }
            }

            foreach (var pair in points)
            {
                var v = (int)(Math.Sqrt(pair.Value) * size);
                var coord = pair.Key.Value;

                g.DrawEllipse(LinkBrush, new Pen(LinkBrush), new Point(coord.X - v / 2, -coord.Y - v / 2).MapToScreen(), v, v);
                g.DrawText(TextBrush, new Point(coord.X - v, -coord.Y - v/2 - 15).MapToScreen(), 
                    new FormattedText(pair.Value.ToString(), Typeface.Default, 20, TextAlignment.Center, TextWrapping.NoWrap, new Size(v,v)));
            }
            // HashSet<(Facility,Facility)> set = new HashSet<(Facility, Facility)>();

            // foreach (Person person in persons)
            // {
            //     if (person.CurrentAction is Moving moving && moving.Link.To.Coords != null && person.Location?.Coords != null && person.Location != null)
            //     {
            //         set.Add((person.Location, moving.Link.To));
            //     }
            // }
            //
            // foreach (var link in set)
            // {
            //     if (link.Item1.Coords != null && link.Item2.Coords != null)
            //     {
            //         int sizeX = link.Item1.Size?.X ?? Renderer.DefaultSize.X;
            //         int sizeY = link.Item1.Size?.Y ?? Renderer.DefaultSize.Y;
            //
            //         g.DrawLine(LinkPen,
            //             link.Item1.Coords.X + sizeX / 2, link.Item1.Coords.Y + sizeY / 2,
            //             link.Item2.Coords.X + sizeX / 2, link.Item2.Coords.Y + sizeY / 2);
            //     }
            // }
        }
    }
}
