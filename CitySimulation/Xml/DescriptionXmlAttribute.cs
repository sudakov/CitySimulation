using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Xml
{
    public class DescriptionXmlAttribute : Attribute
    {
        public string Text { get; private set; }

        public DescriptionXmlAttribute(string text)
        {
            Text = text;
        }
    }
}
