using System;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CitySimulation.Entities;

namespace SimulationCrossplatform.Render
{
    public class FacilityRenderer : Renderer
    {
        public IBrush Brush;
        public IBrush TextBrush = Brushes.Black;


        public override void Render(EntityBase entity, DrawingContext g, Func<Facility, string> dataSelector = null, Func<Facility, IBrush> colorSelector = null)
        {
            Facility facility = (Facility) entity;
            
            if (facility.Polygon != null)
            {
                Geometry geometry = new PolylineGeometry(facility.Polygon.Select(x => new Point(x.X, -x.Y).MapToScreen()), false);

                IBrush brush = colorSelector?.Invoke(facility) ?? Brush;
                g.DrawGeometry(brush, new Pen(brush), geometry);

                // string data = dataSelector?.Invoke(facility) ?? facility.PersonsCount.ToString();
                // g.DrawText(TextBrush, new Point(facility.Coords.X, -facility.Coords.Y), FormatText(data, DefaultFont));

                // string str = facility.Name;
                // g.DrawText(TextBrush, new Point(facility.Coords.X, -facility.Coords.Y + DefaultFont.Size * 2f), FormatText(str, BoldFont));
            }
            else
            {
                Point size = facility.Size != CitySimulation.Tools.Point.Zero ? new Point(facility.Size.X, facility.Size.Y) : DefaultSize;

                g.FillRectangle(colorSelector?.Invoke(facility) ?? Brush, new Rect(facility.Coords.X, -facility.Coords.Y,
                    size.X,
                    size.Y).MapToScreen());
                // g.DrawRectangle(new Pen(Brush), new Rect(facility.Coords.X, -facility.Coords.Y,
                //     size.X,
                //     size.Y).MapToScreen());

                // string data = dataSelector?.Invoke(facility) ?? facility.PersonsCount.ToString();
                // g.DrawText(TextBrush, new Point(facility.Coords.X, -facility.Coords.Y), FormatText(data, DefaultFont));

                // string str = facility.ToLogString();
                //
                // if (size.Y > BoldFont.Size * 3)
                // {
                //     g.DrawText(TextBrush, new Point(facility.Coords.X, -facility.Coords.Y + size.Y - BoldFont.Size * 3f), FormatText(str, BoldFont));
                // }
            }
        }

        public override void RenderText(EntityBase facility, DrawingContext g, Func<Facility, string> dataSelector = null, Func<Facility, IBrush> colorSelector = null)
        {
            string str = facility.Name;
            g.DrawText(TextBrush, new Point(facility.Coords.X, -facility.Coords.Y + DefaultFont.Size * 2f).MapToScreen(), FormatText(str, BoldFont));
        }
    }
}
