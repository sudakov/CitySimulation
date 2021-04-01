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

        public override void Render(Entity entity, Graphics g, RenderParams renderParams)
        {
            Render((Bus)entity, g, renderParams);
        }

        public void Render(Bus bus, Graphics g, RenderParams renderParams)
        {
            int size = renderParams.FacilitySize;
            Point coords = null;
            if (bus.Action is Moving moving)
            {
                coords = moving.Link.From.Coords + (moving.Link.To.Coords - moving.Link.From.Coords) * moving.DistanceCovered / (int)moving.Link.Length;
                coords = new Point((int)(coords.X * renderParams.Scale) + Offset.X, (int)(coords.Y * renderParams.Scale) + Offset.Y);

                g.FillRectangle(Brush, coords.X, coords.Y, size, size);
            }
            else if(bus.Action is Waiting waiting)
            {
                if (bus.Station != null)
                {
                    try
                    {
                        coords = new Point((int)(bus.Station.Coords.X * renderParams.Scale) + Offset.X, (int)(bus.Station.Coords.Y * renderParams.Scale) + Offset.Y);
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
                g.DrawString(bus.PersonsCount.ToString(), SystemFonts.DefaultFont, TextBrush, coords.X, coords.Y);
                g.DrawString(bus.Name, BoldFont, TextBrush, coords.X, coords.Y + size - 15);
            }
        }
    }
}
