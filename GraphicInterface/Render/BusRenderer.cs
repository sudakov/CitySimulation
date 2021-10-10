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

        [System.Diagnostics.DebuggerHidden]
        public override void Render(EntityBase entity, Graphics g, Func<Facility, string> dataSelector = null, Func<Facility, Brush> colorSelector = null)
        {
            var bus = (Bus) entity;

            int size = DefaultSize.X;
            Point coords = null;
            if (bus.Action is Moving moving)
            {
                var toCoords = moving.Link.To.Coords;
                var fromCoords = moving.Link.From.Coords;

                coords = fromCoords + (toCoords - fromCoords) * moving.DistanceCovered / (int)moving.Link.Length;
                coords = new Point((int)(coords.X) + Offset.X, (int)(coords.Y) + Offset.Y);

                g.FillRectangle(colorSelector?.Invoke(bus) ?? Brush, coords.X, coords.Y, size, size);
                g.DrawRectangle(new Pen(Brush), coords.X, coords.Y, size, size);
            }
            else if (bus.Action is Waiting waiting)
            {
                if (bus.Station != null)
                {
                    try
                    {
                        coords = new Point((int)(bus.Station.Coords.X) + Offset.X, (int)(bus.Station.Coords.Y) + Offset.Y);
                        g.FillRectangle(colorSelector?.Invoke(bus) ?? Brush, coords.X, coords.Y, size, size);
                        g.DrawRectangle(new Pen(Brush), coords.X, coords.Y, size, size);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                }
            }

            if (coords != null)
            {
                string data = dataSelector?.Invoke(bus) ?? bus.PersonsCount.ToString();

                g.DrawString(data, DefaultFont, TextBrush, coords.X, coords.Y);
                g.DrawString(bus.Name, BoldFont, TextBrush,
                    coords.X,
                    coords.Y + size - DefaultFont.Size * 1.5f);
            }
        }

    }
}
