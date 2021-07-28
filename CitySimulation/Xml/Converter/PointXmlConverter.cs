using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using CitySimulation.Tools;

namespace CitySimulation.Xml
{
    public class PointXmlConverter : XmlConverter<Point>
    {
        protected override void WriteObj(XmlWriter writer, Point obj)
        {
            writer.WriteValue($"{obj.X},{obj.Y}");
        }

        protected override Point ReadObj(XmlReader reader)
        {
            var val = reader.ReadElementContentAsString();
            var split = val.Split(',');

            return new Point(int.Parse(split[0]), int.Parse(split[1]));
        }
    }
}
