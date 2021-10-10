using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entities;

namespace GraphicInterface.Render
{
    public class PersonsRenderer
    {
        public Pen LinkPen = Pens.LawnGreen;
        private int delta = 50;
        private int size = 10;
        public void Render(IEnumerable<Person> persons, Graphics g)
        {
            Dictionary<CitySimulation.Tools.Point, int> points = new Dictionary<CitySimulation.Tools.Point, int>();

            foreach (var person in persons)
            {
                var coords = person.CalcCoords();
                if (coords != null)
                {
                    var point = points.Keys.FirstOrDefault(x => CitySimulation.Tools.Point.Distance(coords, x) < delta);
                    if (point != null)
                    {
                        points[point] += 1;
                    }
                    else
                    {
                        points.Add(coords, 1);
                    }
                }
            }

            foreach (var pair in points)
            {
                g.FillEllipse(new SolidBrush(LinkPen.Color), pair.Key.X - pair.Value * size / 2, pair.Key.Y - pair.Value * size / 2, pair.Value * size, pair.Value * size);
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
