using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using CitySimulation.Entities;

namespace SimulationCrossplatform.Render
{
    public class RoutesRenderer
    {
        public IBrush TextBrush = Brushes.Black;
        public IBrush LinkBrush = Brushes.BlueViolet;

        public void Render(Dictionary<string, List<Station>> routes, DrawingContext g)
        {
            var pen = new Pen(LinkBrush, 3);
            var constraint = new Size(20, 20);

            foreach (var (name, stations) in routes)
            {
                for (int i = 0; i < stations.Count; i++)
                {
                    g.DrawLine(pen, stations[i].Coords.ToAvaloniaPoint().MapToScreen(), stations[i+1 != stations.Count ? i+1 : 0].Coords.ToAvaloniaPoint().MapToScreen());
                }

                g.DrawText(LinkBrush, stations[0].Coords.ToAvaloniaPoint().MapToScreen(), new FormattedText(name, Typeface.Default, 20, TextAlignment.Center, TextWrapping.NoWrap, constraint));
            }
        }
    }
}
