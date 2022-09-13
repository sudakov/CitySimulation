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

        public IBrush WaitingBrush;

        // [System.Diagnostics.DebuggerHidden]
        public override void Render(EntityBase entity, DrawingContext g, Func<Facility, string> dataSelector = null, Func<Facility, IBrush> colorSelector = null)
        {
            var bus = (Transport) entity;

            Point? coords = bus.CalcCoords();
            if (coords != null)
            {
                var rect = new Rect(coords.Value.ToAvaloniaPoint(), new Size(DefaultSize.X, DefaultSize.Y)).MapToScreen();
                g.FillRectangle(colorSelector?.Invoke(bus) ?? Brush, rect);
                g.DrawRectangle(new Pen(Brush), rect);
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
