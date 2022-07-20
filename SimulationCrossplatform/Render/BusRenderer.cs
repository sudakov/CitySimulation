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
        public Point Offset = new Point(0, 0);

        public IBrush WaitingBrush = Brushes.Aqua;

        // [System.Diagnostics.DebuggerHidden]
        public override void Render(EntityBase entity, DrawingContext g, Func<Facility, string> dataSelector = null, Func<Facility, IBrush> colorSelector = null)
        {
            var bus = (Transport) entity;

            double size = DefaultSize.X;
            Point coords = null;
            if (bus.Action is Moving moving)
            {
                var toCoords = moving.Link.To.Coords;
                var fromCoords = moving.Link.From.Coords;

                coords = fromCoords + (toCoords - fromCoords) * moving.DistanceCovered / (int)moving.Link.Length;
                coords = new Point((int)(coords.X) + Offset.X, (int)(coords.Y) + Offset.Y);

                g.FillRectangle(colorSelector?.Invoke(bus) ?? Brush, new Rect(coords.X, coords.Y, size, size));
                g.DrawRectangle(new Pen(Brush), new Rect(coords.X, coords.Y, size, size));
            }
            else if (bus.Action is Waiting waiting)
            {
                var station = bus.Station;
                if (station != null)
                {
                    coords = new Point((int)(station.Coords.X) + Offset.X, (int)(station.Coords.Y) + Offset.Y);
                    g.FillRectangle(colorSelector?.Invoke(bus) ?? Brush, new Rect(coords.X, coords.Y, size, size));
                    g.DrawRectangle(new Pen(Brush), new Rect(coords.X, coords.Y, size, size));
                }
            }

            if (coords != null)
            {
                string data = dataSelector?.Invoke(bus) ?? bus.PersonsCount.ToString();

                g.DrawText(TextBrush, new Avalonia.Point(coords.X, coords.Y), FormatText(data, DefaultFont));
                g.DrawText(TextBrush, new Avalonia.Point(coords.X, coords.Y + size - DefaultFont.Size * 1.5f), FormatText(bus.Name, BoldFont));
            }
        }

    }
}
