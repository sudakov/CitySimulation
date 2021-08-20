using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using CitySimulation.Entities;

namespace GraphicInterface.Render
{
    public class FacilityRenderer : Renderer
    {
        public Brush Brush;
        public Brush TextBrush = Brushes.Black;


        public override void Render(EntityBase entity, Graphics g, Func<Facility, string> dataSelector = null, Func<Facility, Brush> colorSelector = null)
        {
            Facility facility = (Facility) entity;

            Point size = facility.Size != null ? new Point(facility.Size.X, facility.Size.Y) : DefaultSize;

            g.FillRectangle(colorSelector?.Invoke(facility) ?? Brush, facility.Coords.X, facility.Coords.Y,
                size.X,
                size.Y);
            g.DrawRectangle(new Pen(Brush), facility.Coords.X, facility.Coords.Y,
                size.X,
                size.Y);

            string data = dataSelector?.Invoke(facility) ?? facility.PersonsCount.ToString();

            g.DrawString(data, DefaultFont, TextBrush, 
                facility.Coords.X, 
                facility.Coords.Y);

            if (size.Y > DefaultFont.Size*3)
            {
                g.DrawString(facility.Name, BoldFont, TextBrush,
                    facility.Coords.X,
                    facility.Coords.Y + size.Y - DefaultFont.Size*1.5f);
            }
        }
    }
}
