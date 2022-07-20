using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;

namespace SimulationCrossplatform
{
    public static class OsmDrawTools
    {
        public static Point MapToScreen(this Point point)
        {
            return new Point(point.X, point.Y * 1.5);
        }

        public static Point ScreenToMap(this Point point)
        {
            return new Point(point.X, point.Y / 1.5);
        }

        public static Rect MapToScreen(this Rect rect)
        {
            return new Rect(rect.X, rect.Y * 1.5, rect.Width, rect.Height * 1.5);
        }

        public static Rect ScreenToMap(this Rect rect)
        {
            return new Rect(rect.X, rect.Y / 1.5, rect.Width, rect.Height / 1.5);
        }
    }
}
