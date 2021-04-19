using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using CitySimulation.Entity;

namespace GraphicInterface.Render
{
    public abstract class Renderer
    {
        public static Font DefaultFont = new Font(SystemFonts.DefaultFont.FontFamily, 20);
        public static Font BoldFont = new Font(SystemFonts.DefaultFont.FontFamily, 20, FontStyle.Bold);
        public static Point DefaultSize = new Point(60, 60);

        public virtual void Render(Entity facility, Graphics g, Func<Facility, string> dataSelector = null, Func<Facility, Brush> colorSelector = null) { }

    }
}
