using System.Collections.Generic;
using System.Text;
using System.Xml;
using CitySimulation.Tools;

namespace CitySimulation.Xml
{
    public class RangeXmlConverter : XmlConverter<Range>
    {
        protected override void WriteObj(XmlWriter writer, Range obj)
        {
            writer.WriteValue($"{obj.Start}:{obj.End}{(obj.Reverse ? ":reverse" : "")}");
        }

        protected override Range ReadObj(XmlReader reader)
        {
            var val = reader.ReadElementContentAsString();
            var split = val.Split(':');

            return new Range(int.Parse(split[0]), int.Parse(split[1])){ Reverse = split.Length > 2 && split[2] == "reverse" };
        }
    }
}
