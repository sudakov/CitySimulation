using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entities;
using CitySimulation.Ver1.Entity;
using Point = CitySimulation.Tools.Point;

namespace GraphicInterface.Render
{
    public class BusRenderer : FacilityRenderer
    {
        public Point Offset = new Point(0, 0);

        public Brush WaitingBrush = Brushes.Aqua;

        // [System.Diagnostics.DebuggerHidden]
        public override void Render(EntityBase entity, Graphics g, Func<Facility, string> dataSelector = null, Func<Facility, Brush> colorSelector = null)
        {
            void DrawStrings(Transport transport, Point point, int i)
            {
                string data = dataSelector?.Invoke(transport) ?? transport.PersonsCount.ToString();

                g.DrawString(data, DefaultFont, TextBrush, point.X, point.Y);
                g.DrawString(transport.Name, BoldFont, TextBrush, point.X, point.Y + i - DefaultFont.Size * 1.5f);
            }

            var bus = (Transport) entity;

            int size = DefaultSize.X;
            if (bus.Action is Moving moving)
            {
                var toCoords = moving.Link.To.Coords;
                var fromCoords = moving.Link.From.Coords;

                Point coords = fromCoords + (toCoords - fromCoords) * moving.DistanceCovered / (int)moving.Link.Length;
                coords = new Point((int)(coords.X) + Offset.X, (int)(coords.Y) + Offset.Y);

                g.FillRectangle(colorSelector?.Invoke(bus) ?? Brush, coords.X, coords.Y, size, size);
                g.DrawRectangle(new Pen(Brush), coords.X, coords.Y, size, size);
                DrawStrings(bus, coords, size);
            }
            else if (bus.Action is Waiting waiting)
            {
                var station = bus.Station;
                if (station != null)
                {
                    Point coords = new Point((int)(station.Coords.X) + Offset.X, (int)(station.Coords.Y) + Offset.Y);
                    g.FillRectangle(colorSelector?.Invoke(bus) ?? Brush, coords.X, coords.Y, size, size);
                    g.DrawRectangle(new Pen(Brush), coords.X, coords.Y, size, size);
                    DrawStrings(bus, coords, size);
                }
            }
        }

    }
}
