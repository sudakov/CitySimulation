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

        public void Render(List<Person> persons, Graphics g, RenderParams renderParams)
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
                    g.DrawLine(LinkPen,
                        link.Item1.Coords.X * renderParams.Scale + renderParams.FacilitySize / 2, link.Item1.Coords.Y * renderParams.Scale + renderParams.FacilitySize / 2,
                        link.Item2.Coords.X * renderParams.Scale + renderParams.FacilitySize / 2, link.Item2.Coords.Y * renderParams.Scale + renderParams.FacilitySize / 2);
                }
            }

        }
    }
}
