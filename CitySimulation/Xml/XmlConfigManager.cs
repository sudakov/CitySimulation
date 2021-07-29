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
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Vml;

namespace CitySimulation.Tools
{
    public class XmlConfigManager
    {
        private Dictionary<Type, XmlConverter> converters = new Dictionary<Type, XmlConverter>();

        public void AddConverter<T>(XmlConverter<T> converter)
        {
            converters.Add(typeof(T), converter);
        }

        private HashSet<object> GetDublicates(object obj, List<object> objectsList)
        {
            HashSet<object> res = null;
            if (!obj.GetType().IsValueType)
            {
                if (objectsList.Contains(obj))
                {
                    res = new HashSet<object>() {obj};
                }
                else
                {
                    objectsList.Add(obj);


                    if (obj is ITuple tuple)
                    {
                        for (int i = 0; i < tuple.Length; i++)
                        {
                            var subRes = GetDublicates(tuple[i], objectsList);
                            if (res == null)
                            {
                                res = subRes;
                            }
                            else if (subRes != null)
                            {
                                res.UnionWith(subRes);
                            }
                        }
                    }
                    else if (obj is IList collection)
                    {
                        foreach (var o in collection)
                        {
                            var subRes = GetDublicates(o, objectsList);
                            if (res == null)
                            {
                                res = subRes;
                            }
                            else if (subRes != null)
                            {
                                res.UnionWith(subRes);
                            }
                        }
                    }
                    else
                    {
                        foreach (var propertyInfo in obj.GetType().GetProperties().Where(x => x.CanRead && x.CanWrite))
                        {
                            var value = propertyInfo.GetValue(obj);
                            var subRes = GetDublicates(value, objectsList);
                            if (res == null)
                            {
                                res = subRes;
                            }
                            else if (subRes != null)
                            {
                                res.UnionWith(subRes);
                            }
                        }
                    }
                }
            }

            return res;
        }

        public void WriteObject(object obj, Stream stream)
        {
            XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings()
            {
                NewLineChars = "\n",
                Indent = true,
            });

            Dictionary<object, Guid> dublicates = GetDublicates(obj, new List<object>()).ToDictionary(x => x,x=> Guid.Empty);   


            writer.WriteStartDocument();
            writer.WriteStartElement("root");

            WriteObject(writer, obj, null, dublicates);

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
        }

        private void WriteObject(XmlWriter writer, object obj, PropertyInfo property, Dictionary<object, Guid> dublicates)
        {
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
                    WriteComplexObject(writer, obj, dublicates);
                }
            }
            else if (obj is ITuple tuple)
            {
                if (!tuple.GetType().GenericTypeArguments[0].IsValueType)
                {
                    throw new NotImplementedException("Only ValueType Tuples supported");
                }

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

                if (dublicates.ContainsKey(obj) && dublicates[obj] != Guid.Empty)
                {
                    writer.WriteAttributeString("guid", dublicates[obj].ToString());
                }
                else
                {
                    if (dublicates.ContainsKey(obj))
                    {
                        dublicates[obj] = Guid.NewGuid();
                        writer.WriteAttributeString("guid", dublicates[obj].ToString());
                    }

                    foreach (var o in collection)
                    {
                        writer.WriteStartElement("value");
                        WriteObject(writer, o, null, dublicates);
                        writer.WriteEndElement();
                    }
                }

                
            }
            else
            {
                WriteComplexObject(writer, obj, dublicates);
            }

            

        }

        private void WriteComplexObject(XmlWriter writer, object obj, Dictionary<object, Guid> dublicates)
        {
            writer.WriteAttributeString("type", obj.GetType().FullName);

            if (dublicates.ContainsKey(obj) && dublicates[obj] != Guid.Empty)
            {
                writer.WriteAttributeString("guid", dublicates[obj].ToString());
            }
            else
            {
                if (dublicates.ContainsKey(obj))
                {
                    dublicates[obj] = Guid.NewGuid();
                    writer.WriteAttributeString("guid", dublicates[obj].ToString());
                }

                foreach (var propertyInfo in obj.GetType().GetProperties().Where(x => x.CanRead && x.CanWrite))
                {
                    if(propertyInfo.GetCustomAttribute<IgnoreXmlAttribute>() != null)
                    {
                        continue;
                    }

                    var description = propertyInfo.GetCustomAttribute<DescriptionXmlAttribute>();

                    if (description != null)
                    {
                        writer.WriteComment(description.Text);
                    }

                    var value = propertyInfo.GetValue(obj);
                    writer.WriteStartElement(propertyInfo.Name);
                    WriteObject(writer, value, propertyInfo, dublicates);

                    writer.WriteEndElement();
                }
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
                    res = Read(subreader, typeof(T), new Dictionary<Guid, object>());
                }
            }

            return (T)res;
        }

        private object Read(XmlReader reader, Type type, Dictionary<Guid, object> dublicates)
        {
            var guid = reader.GetAttribute("guid");
            Guid? gGuid = null;

            if (guid != null)
            {
                gGuid = Guid.Parse(guid);
                if (dublicates.ContainsKey(gGuid.Value))
                {
                    return dublicates[gGuid.Value];
                }
            }

            object result;

            if (reader.Name != "root" && new[] { typeof(string), typeof(int), typeof(float), typeof(double), typeof(long), typeof(Enum), typeof(bool), typeof(DateTime) }.Contains(type))
            {
                var val = reader.ReadElementContentAs(type, null);
                result = val;
            }
            else if (reader.Name != "root" && converters.Keys.Any(x=>x.IsAssignableFrom(type)))
            {
                var converter = converters.GetValueOrDefault(type, converters.First(x=>x.Key.IsAssignableFrom(type)).Value);
                var val = converter.Read(reader);


                if (guid != null)
                {
                    dublicates.Add(Guid.Parse(guid), val);
                }


                if (converter.DefaultReading)
                {
                    ReadComplexObject(reader, val.GetType(), ref val, dublicates);
                }

                result = val;

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

                if (guid != null)
                {
                    dublicates.Add(Guid.Parse(guid), obj);
                }

                result = obj;
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

                if (guid != null)
                {
                    dublicates.Add(Guid.Parse(guid), obj);
                }

                ReadComplexObject(reader, type, ref obj, dublicates);
                result = obj;
            }

            return result;
        }

        private void ReadComplexObject(XmlReader xmlReader, Type type, ref object obj, Dictionary<Guid, object> dublicates)
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

                        var val = Read(subReader, type.GetElementType(), dublicates);

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

                        var val = Read(subReader, attr != null ? Type.GetType(attr) : obj.GetType().GetGenericArguments().Single(), dublicates);
                        list.Add(val);
                    }
                    else
                    {
                        var propertyInfo = type.GetProperty(xmlReader.Name);
                        var subreader = xmlReader.ReadSubtree();
                        subreader.Read();
                        var val = Read(subreader, propertyInfo.PropertyType, dublicates);
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

        private static void Resize(ref Array array, int newSize)
        {
            Type elementType = array.GetType().GetElementType();
            Array newArray = Array.CreateInstance(elementType, newSize);
            Array.Copy(array, newArray, Math.Min(array.Length, newArray.Length));
            array = newArray;
        }

    }

 

}
