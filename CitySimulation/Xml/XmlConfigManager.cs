using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using CitySimulation.Xml;
using DocumentFormat.OpenXml.Office2010.ExcelAc;

namespace CitySimulation.Tools
{
    public class XmlConfigManager
    {
        private Dictionary<Type, XmlConverter> converters = new Dictionary<Type, XmlConverter>();

        public void AddConverter<T>(XmlConverter<T> converter)
        {
            converters.Add(typeof(T), converter);
        }

        public void WriteObject(object obj, Stream stream)
        {
            XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings()
            {
                NewLineChars = "\n",
                Indent = true,
            });


            writer.WriteStartDocument();
            writer.WriteStartElement("root");

            WriteObject(writer, obj, null, new Dictionary<object, Guid>());

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
        }

        private void WriteObject(XmlWriter writer, object obj, PropertyInfo property, Dictionary<object, Guid> objects)
        {
            void WriteComplexObject(XmlWriter xmlWriter, object obj, Dictionary<object, Guid> dictionary)
            {
                writer.WriteAttributeString("type", obj.GetType().FullName);

                if (!dictionary.ContainsKey(obj))
                {
                    dictionary.Add(obj, Guid.NewGuid());
                }
                else
                {
                    throw new DataException("Recursive configuration");
                }

                foreach (var propertyInfo in obj.GetType().GetProperties().Where(x => x.CanRead && x.CanWrite))
                {
                    var value = propertyInfo.GetValue(obj);
                    xmlWriter.WriteStartElement(propertyInfo.Name);
                    WriteObject(xmlWriter, value, propertyInfo, dictionary);

                    xmlWriter.WriteEndElement();
                }
            }

            Type type = obj?.GetType();
            if (obj == null)
            {
                
            }
            else if (new[] { typeof(string), typeof(int), typeof(float), typeof(double), typeof(long), typeof(Enum), typeof(bool), typeof(DateTime) }.Contains(obj.GetType()))
            {
                writer.WriteValue(obj);
            }
            else if (converters.Keys.Any(x=>x.IsAssignableFrom(type)))
            {
                var converter = converters.GetValueOrDefault(type, converters.First(x => x.Key.IsAssignableFrom(type)).Value);

                converter.Write(writer, obj);

                if (converter.DefaultWriting)
                {
                    WriteComplexObject(writer, obj, objects);
                }
            }
            else if (obj is ITuple tuple)
            {
                List<object> items = new List<object>();
                for (int i = 0; i < tuple.Length; i++)
                {
                    items.Add(tuple[i]);
                }

                writer.WriteValue(String.Join(",", items));
            }
            else if (obj is IList collection)
            {
                writer.WriteAttributeString("type", obj.GetType().FullName);

                if (!objects.ContainsKey(obj))
                {
                    objects.Add(obj, Guid.NewGuid());
                }
                else
                {
                    throw new DataException("Recursive configuration");
                }

                foreach (var o in collection)
                {
                    writer.WriteStartElement("value");
                    // writer.WriteAttributeString("type", o.GetType().FullName);
                    WriteObject(writer, o, null, objects);
                    writer.WriteEndElement();
                }
            }
            else
            {
                WriteComplexObject(writer, obj, objects);
            }

            

        }

        public T ReadObject<T>(Stream stream)
        {
            XmlReader reader = XmlReader.Create(stream);

            object res = null;
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "root")
                {
                    var subreader = reader.ReadSubtree();
                    subreader.Read();
                    res = Read(subreader, typeof(T));
                }
            }

            return (T)res;
        }

        private object Read(XmlReader reader, Type type)
        {
            void ReadComplexObject(XmlReader xmlReader, Type type1, ref object obj)
            {
                int k = 0;
                while (xmlReader.Read())
                {
                    if (xmlReader.NodeType == XmlNodeType.Element)
                    {
                        if (obj is Array array)
                        {
                            if (array.Length <= k)
                            {
                                Resize(ref array, array.Length * 2);
                            }

                            var subReader = xmlReader.ReadSubtree();
                            subReader.Read();
                            subReader.ReadSubtree();
                            subReader = subReader.ReadSubtree();
                            subReader.Read();

                            var val = Read(subReader, type1.GetElementType());

                            array.SetValue(val, k);
                        }
                        else if (obj is IList list)
                        {
                            string attr = xmlReader.GetAttribute("type");

                            var subReader = xmlReader.ReadSubtree();
                            subReader.Read();
                            subReader.ReadSubtree();
                            subReader = subReader.ReadSubtree();
                            subReader.Read();

                            var val = Read(subReader,
                                attr != null ? Type.GetType(attr) : obj.GetType().GetGenericArguments().Single());
                            list.Add(val);
                        }
                        else
                        {
                            var propertyInfo = type1.GetProperty(xmlReader.Name);
                            var subreader = xmlReader.ReadSubtree();
                            subreader.Read();
                            var val = Read(subreader, propertyInfo.PropertyType);
                            propertyInfo.SetValue(obj, val);
                        }

                        k++;
                    }
                }

                if (obj is Array array2)
                {
                    Resize(ref array2, k);
                    obj = array2;
                }
            }

            if (reader.Name != "root" && new[] { typeof(string), typeof(int), typeof(float), typeof(double), typeof(long), typeof(Enum), typeof(bool), typeof(DateTime) }.Contains(type))
            {
                var val = reader.ReadElementContentAs(type, null);
                return val;
            }
            else if (reader.Name != "root" && converters.Keys.Any(x=>x.IsAssignableFrom(type)))
            {
                var converter = converters.GetValueOrDefault(type, converters.First(x=>x.Key.IsAssignableFrom(type)).Value);
                var val = converter.Read(reader);

                if (converter.DefaultReading)
                {
                    ReadComplexObject(reader, val.GetType(), ref val);
                }

                return val;
            }
            else if (reader.Name != "root" && typeof(ITuple).IsAssignableFrom(type))
            {
                string[] vals = reader.ReadElementContentAsString().Split(',');

                var elementsType = type.GetGenericArguments();

                object[] res = new object[vals.Length];

                for (var i = 0; i < vals.Length; i++)
                {
                    res[i] = Convert.ChangeType(vals[i], elementsType[i]);
                }

                var obj = Activator.CreateInstance(type, res);

                return obj;
            }
            else
            {
                string attrType = reader.GetAttribute("type");

                object obj;
                if (attrType != null)
                {
                    type = Type.GetType(attrType);
                }

                if (type.IsArray)
                {
                    obj = Activator.CreateInstance(type, 16);
                }
                else
                {
                    obj = Activator.CreateInstance(type);
                }

                ReadComplexObject(reader, type, ref obj);
                return obj;
            }
        }

        private static void Resize(ref Array array, int newSize)
        {
            Type elementType = array.GetType().GetElementType();
            Array newArray = Array.CreateInstance(elementType, newSize);
            Array.Copy(array, newArray, Math.Min(array.Length, newArray.Length));
            array = newArray;
        }

    }

 

}
