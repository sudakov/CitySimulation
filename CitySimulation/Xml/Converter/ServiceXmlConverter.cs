using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using CitySimulation.Ver1.Entity;

namespace CitySimulation.Xml
{
    public class ServiceXmlConverter : XmlConverter<Service>
    {
        public override bool DefaultWriting => true;
        public override bool DefaultReading => true;

        protected override void WriteObj(XmlWriter writer, Service obj)
        {
            writer.WriteAttributeString("name", obj.Name);
        }

        protected override Service ReadObj(XmlReader reader)
        {
            var nameStr = reader.GetAttribute("name");
            var typeStr = reader.GetAttribute("type");
            return (Service)Activator.CreateInstance(Type.GetType(typeStr), nameStr);
        }
    }
}
