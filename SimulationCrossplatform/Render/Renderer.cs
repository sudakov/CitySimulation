using System;
using Avalonia.Media;
using CitySimulation.Entities;
using FontFamily = Avalonia.Media.FontFamily;
using Size = Avalonia.Size;
using Point = Avalonia.Point;

namespace SimulationCrossplatform.Render
{
    public abstract class Renderer
    {
        public static Typeface DefaultFont = new Typeface(FontFamily.Default, Avalonia.Media.FontStyle.Normal, FontWeight.Normal);
        public static Typeface BoldFont = new Typeface(FontFamily.Default, Avalonia.Media.FontStyle.Normal, FontWeight.Bold);
        public static int DefaultFontSize = 15;
        public static readonly Point DefaultSize = new Point(20, 20);

        public virtual void Render(EntityBase facility, DrawingContext g, Func<Facility, string> dataSelector = null, Func<Facility, IBrush> colorSelector = null) { }
        public virtual void RenderText(EntityBase facility, DrawingContext g, Func<Facility, string> dataSelector = null, Func<Facility, IBrush> colorSelector = null) { }

        public static FormattedText FormatText(string text, Typeface font, int fontSize)
        {
            return new FormattedText(text, font, fontSize, TextAlignment.Left, TextWrapping.NoWrap, Size.Empty);
        }

    }
}
