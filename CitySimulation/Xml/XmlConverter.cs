using CitySimulation.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace CitySimulation.Xml
{
    public abstract class XmlConverter
    {
        /// <summary>
        /// Продолжить запись как остальные объекты
        /// </summary>
        public virtual bool DefaultWriting => false;
        public virtual bool DefaultReading => false;

        public abstract void Write(XmlWriter writer, object obj);
        public abstract object Read(XmlReader writer);
    }

    public abstract class XmlConverter<T> : XmlConverter
    {
        public override void Write(XmlWriter writer, object obj)
        {
            WriteObj(writer, (T)obj);
        }

        public override object Read(XmlReader reader)
        {
            return ReadObj(reader);
        }

        protected abstract void WriteObj(XmlWriter writer, T obj);

        protected abstract T ReadObj(XmlReader reader);
    }
}
