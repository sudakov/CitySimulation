using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entity;

namespace GraphicInterface.Render
{
    public class PersonsRenderer
    {
        public Pen LinkPen = Pens.OrangeRed;

        public void Render(IEnumerable<Person> persons, Graphics g)
        {
            HashSet<(Facility,Facility)> set = new HashSet<(Facility, Facility)>();

            foreach (Person person in persons)
            {
                if (person.CurrentAction is Moving moving && moving.Link.To.Coords != null && person.Location?.Coords != null)
                {
                    set.Add((person.Location, moving.Link.To));
                }
            }

            foreach (var link in set)
            {
                if (link.Item1.Coords != null && link.Item2.Coords != null)
                {
                    int sizeX = link.Item1.Size?.X ?? Renderer.DefaultSize.X;
                    int sizeY = link.Item1.Size?.Y ?? Renderer.DefaultSize.Y;

                    g.DrawLine(LinkPen,
                        link.Item1.Coords.X + sizeX / 2, link.Item1.Coords.Y + sizeY / 2,
                        link.Item2.Coords.X + sizeX / 2, link.Item2.Coords.Y + sizeY / 2);
                }
            }

        }
    }
}
