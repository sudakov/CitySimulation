using System;
using System.Drawing;
using Avalonia.Media;
using CitySimulation.Entities;
using FontFamily = Avalonia.Media.FontFamily;
using FontStyle = System.Drawing.FontStyle;
using Size = Avalonia.Size;
using Point = Avalonia.Point;

namespace SimulationCrossplatform.Render
{
    public abstract class Renderer
    {
        public static Font DefaultFont = new Font(SystemFonts.DefaultFont.FontFamily, 15);
        public static Font BoldFont = new Font(SystemFonts.DefaultFont.FontFamily, 15, FontStyle.Bold);
        public static Point DefaultSize = new Point(20, 15);

        public virtual void Render(EntityBase facility, DrawingContext g, Func<Facility, string> dataSelector = null, Func<Facility, IBrush> colorSelector = null) { }
        public virtual void RenderText(EntityBase facility, DrawingContext g, Func<Facility, string> dataSelector = null, Func<Facility, IBrush> colorSelector = null) { }

        public static FormattedText FormatText(string text, Font font)
        {
            return new FormattedText(text, new Typeface(FontFamily.Default, Avalonia.Media.FontStyle.Normal, font.Style == FontStyle.Bold ? FontWeight.Bold : FontWeight.Normal), font.Size, TextAlignment.Left, TextWrapping.NoWrap, Size.Empty);
        }
    }
}
