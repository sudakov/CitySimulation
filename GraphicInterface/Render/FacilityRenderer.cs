using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using CitySimulation.Entity;

namespace GraphicInterface.Render
{
    public class FacilityRenderer : Renderer
    {
        public Brush Brush;
        public Brush TextBrush = Brushes.Black;


        public override void Render(Entity entity, Graphics g, Func<Facility, int> dataSelector = null)
        {
            Facility facility = (Facility) entity;

            Point size = facility.Size != null ? new Point(facility.Size.X, facility.Size.Y) : DefaultSize;

            g.FillRectangle(Brush, facility.Coords.X, facility.Coords.Y,
                size.X,
                size.Y);

            int data = dataSelector?.Invoke(facility) ?? facility.PersonsCount;

            g.DrawString(data.ToString(), DefaultFont, TextBrush, 
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
