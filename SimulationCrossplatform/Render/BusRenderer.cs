using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Media;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entities;
using Point = CitySimulation.Tools.Point;

namespace SimulationCrossplatform.Render
{
    public class BusRenderer : FacilityRenderer
    {
        private static readonly Point BusSize = new Point(20, 10);

        public IBrush WaitingBrush;

        // [System.Diagnostics.DebuggerHidden]
        public override void Render(EntityBase entity, DrawingContext g, Func<Facility, string> dataSelector = null, Func<Facility, IBrush> colorSelector = null)
        {
            var bus = (Transport) entity;

            Point? coords = bus.CalcCoords();
            Point? offsetCoords = bus.CalcOffsetCoords(5);

            if (coords != null)
            {
                if (offsetCoords != null && offsetCoords != coords)
                {
                    var p1 = coords.Value.ToAvaloniaPoint().MapToScreen();
                    var p2 = offsetCoords.Value.ToAvaloniaPoint().MapToScreen();

                    var delta = p2 - p1;
                    double magnitude = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);

                    g.DrawLine(new Pen(Brush, BusSize.Y), p1, p1 + BusSize.X * delta / magnitude);
                }
                else
                {
                    var rect = new Rect(coords.Value.ToAvaloniaPoint().MapToScreen(), new Size(DefaultSize.X, DefaultSize.Y));
                    g.FillRectangle(colorSelector?.Invoke(bus) ?? Brush, rect);
                    g.DrawRectangle(new Pen(Brush), rect);
                }
            }
        }

        public override void RenderText(EntityBase facility, DrawingContext g, Func<Facility, string> dataSelector = null, Func<Facility, IBrush> colorSelector = null)
        {
            var bus = (Transport)facility;

            Point? coords = bus.CalcCoords();
            if (coords != null)
            {
                string data = dataSelector?.Invoke(bus) ?? bus.PersonsCount.ToString();
                g.DrawText(TextBrush, coords.Value.ToAvaloniaPoint().MapToScreen(), FormatText(data, DefaultFont));
                g.DrawText(TextBrush, new Point(coords.Value.X, (int)(coords.Value.Y + DefaultFont.Size * 1.5f)).ToAvaloniaPoint().MapToScreen(), FormatText(bus.Name, BoldFont));
            }
        }
    }
}
