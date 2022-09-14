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
            Dictionary<CitySimulation.Tools.Point?, int> points = new Dictionary<CitySimulation.Tools.Point?, int>();

            foreach (var person in persons)
            {
                var coords = person.CalcCoords();
                if (coords != null)
                {
                    var point = points.Keys.FirstOrDefault(x => CitySimulation.Tools.Point.Distance(coords.Value, x.Value) < delta);
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
                var point = pair.Key.Value;

                g.FillEllipse(new SolidBrush(LinkPen.Color), point.X - v / 2, point.Y - v / 2, v, v);
            }
        }
    }
}
