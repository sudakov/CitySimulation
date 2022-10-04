using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CitySimulation.Entities;
using CitySimulation.Tools;
using Point = Avalonia.Point;

namespace SimulationCrossplatform.Render
{
    public class FacilityRenderer : Renderer
    {
        private Dictionary<EntityBase, Geometry> geometryCache = new Dictionary<EntityBase, Geometry>();
        private IBrush __brush;
        public IBrush Brush
        {
            get => __brush;
            set
            {
                __brush = value;
                _pen = new Pen(value);
            }
        }

        private IPen _pen;
        public IBrush TextBrush = Brushes.Black;


        public override void Render(EntityBase entity, DrawingContext g, Func<Facility, string> dataSelector = null, Func<Facility, IBrush> colorSelector = null)
        {
            Facility facility = (Facility) entity;
            
            if (facility.Polygon != null)
            {
                Geometry geometry = geometryCache.GetOrSetDefault(entity, 
                    () => new PolylineGeometry(
                        facility.Polygon.Select(x => new Point(x.X, -x.Y).MapToScreen()), false));

                IBrush brush = Brush;
                IPen pen = _pen;

                if (colorSelector != null)
                {
                    brush = colorSelector.Invoke(facility);
                    pen = new Pen(brush);
                }
                g.DrawGeometry(brush, pen, geometry);
            }
            else
            {
                Point size = facility.Size != CitySimulation.Tools.Point.Zero ? new Point(facility.Size.X, facility.Size.Y) : DefaultSize;

                g.FillRectangle(colorSelector?.Invoke(facility) ?? Brush, new Rect(facility.Coords.X, -facility.Coords.Y,
                    size.X,
                    size.Y).MapToScreen());
            }
        }

        public override void RenderText(EntityBase facility, DrawingContext g, Func<Facility, string> dataSelector = null, Func<Facility, IBrush> colorSelector = null)
        {
            string str = facility.Name;
            var formattedText = FormatText(str, BoldFont, DefaultFontSize);
            g.DrawText(TextBrush, new Point(facility.Coords.X, -facility.Coords.Y + DefaultFontSize * 2f).MapToScreen(), formattedText);
        }
    }
}
