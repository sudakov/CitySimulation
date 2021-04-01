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

        public Font BoldFont = new Font(SystemFonts.DefaultFont, FontStyle.Bold);

        public override void Render(Entity entity, Graphics g, RenderParams renderParams)
        {
            Facility facility = (Facility) entity;

            Point size = new Point((int)(facility.Size == null ? renderParams.FacilitySize : facility.Size.X * renderParams.Scale), (int)(facility.Size == null ? renderParams.FacilitySize : facility.Size.Y * renderParams.Scale));

            g.FillRectangle(Brush, facility.Coords.X * renderParams.Scale, facility.Coords.Y * renderParams.Scale,
                size.X,
                size.Y);

            g.DrawString(facility.PersonsCount.ToString(), SystemFonts.DefaultFont, TextBrush, 
                facility.Coords.X * renderParams.Scale, 
                facility.Coords.Y * renderParams.Scale);

            if (size.Y > 25)
            {
                g.DrawString(facility.Name, BoldFont, TextBrush,
                    facility.Coords.X * renderParams.Scale,
                    facility.Coords.Y * renderParams.Scale + size.Y - 15);
            }
        }
    }
}
