using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using CitySimulation.Entities;

namespace GraphicInterface.Render
{
    public abstract class Renderer
    {
        public static Font DefaultFont = new Font(SystemFonts.DefaultFont.FontFamily, 15);
        public static Font BoldFont = new Font(SystemFonts.DefaultFont.FontFamily, 15, FontStyle.Bold);
        public static Point DefaultSize = new Point(70, 70);

        public virtual void Render(EntityBase facility, Graphics g, Func<Facility, string> dataSelector = null, Func<Facility, Brush> colorSelector = null) { }

    }
}
