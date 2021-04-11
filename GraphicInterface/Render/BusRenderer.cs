using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entity;
using Point = CitySimulation.Tools.Point;

namespace GraphicInterface.Render
{
    public class BusRenderer : FacilityRenderer
    {
        public Point Offset = new Point(0, 30);

        public Brush WaitingBrush = Brushes.Aqua;

        public override void Render(Entity entity, Graphics g, Func<Facility, int> dataSelector = null)
        {
            Render((Bus)entity, g, dataSelector);
        }

        public void Render(Bus bus, Graphics g, Func<Facility, int> dataSelector = null)
        {
            int size = DefaultSize.X;
            Point coords = null;
            if (bus.Action is Moving moving)
            {
                coords = moving.Link.From.Coords + (moving.Link.To.Coords - moving.Link.From.Coords) * moving.DistanceCovered / (int)moving.Link.Length;
                coords = new Point((int)(coords.X) + Offset.X, (int)(coords.Y) + Offset.Y);

                g.FillRectangle(Brush, coords.X, coords.Y, size, size);
            }
            else if(bus.Action is Waiting waiting)
            {
                if (bus.Station != null)
                {
                    try
                    {
                        coords = new Point((int)(bus.Station.Coords.X) + Offset.X, (int)(bus.Station.Coords.Y) + Offset.Y);
                        g.FillRectangle(Brush, coords.X, coords.Y, size, size);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                }
            }

            if (coords != null)
            {
                int data = dataSelector?.Invoke(bus) ?? bus.PersonsCount;

                g.DrawString(data.ToString(), DefaultFont, TextBrush, coords.X, coords.Y);
                g.DrawString(bus.Name, BoldFont, TextBrush, coords.X, coords.Y + size - 15);
            }
        }
    }
}
