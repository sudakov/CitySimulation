using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CitySimulation.Navigation;
using CitySimulation.Tools;

namespace CitySimulation.Entities
{
    public class FacilityManager : IDictionary<string, Facility>
    {
        private const int FacilityIdOffset = 0;

        private Dictionary<string, Facility> facilities = new Dictionary<string, Facility>();
        private List<Facility> facilities_list = new List<Facility>();

        public FacilityManager()
        {

        }

        public FacilityManager(List<Facility> facilities)
        {
            AddRange(facilities);
        }

        internal List<Facility> GetList()
        {
            return facilities_list;
        }

        public void Add(Facility item)
        {
            Add(item.Name, item);
        }

        public void LinkUnconnected(Facility f1, Facility f2)
        {
            var length = Point.Distance(f1.Coords, f2.Coords);
            f1.Links.Add(new Link(f1, f2, length){Unconnected = true});
            f2.Links.Add(new Link(f2, f1, length){Unconnected = true});
        }

        public void Link(Facility f1, Facility f2, double length, double time)
        {
            f1.Links.Add(new Link(f1,f2, length, time));
            f2.Links.Add(new Link(f2,f1, length, time));
        }

        public void Link(Facility f1, Facility f2, double time)
        {
            Link(f1, f2, Point.Distance(f1.Coords, f2.Coords), time);
        }

        public void Link(Facility f1, Facility f2)
        {
            var length = Point.Distance(f1.Coords, f2.Coords);
            f1.Links.Add(new Link(f1, f2, length));
            f2.Links.Add(new Link(f2, f1, length));
        }

        public RouteTable CreateRouteTable()
        {
            int f_count = facilities_list.Count;
            PathSegment[,] table = new PathSegment[f_count, f_count];

            var facilitiesSpan = CollectionsMarshal.AsSpan(facilities_list);

            for (int i = 0; i < f_count; i++)
            {
                for (int j = 0; j < f_count; j++)
                {
                    Link link = null;
                    foreach (var x in facilitiesSpan[i].Links)
                    {
                        if (!x.Unconnected && x.To == facilitiesSpan[j])
                        {
                            link = x;
                            break;
                        }
                    }

                    if (link != null)
                    {
                        table[i, j] = new PathSegment(link, link.Length, link.Time);
                    }
                }
            }


            for (int i1 = 0; i1 < f_count; i1++)
            {
                for (int i2 = 0; i2 < f_count; i2++)
                {
                    for (int i3 = 0; i3 < f_count; i3++)
                    {
                        if (i2 != i3)
                        {
                            if (table[i2, i1] != null && table[i1, i3] != null && (table[i2, i3] == null || table[i2, i3].TotalTime > table[i2, i1].TotalTime + table[i1, i3].TotalTime))
                            {
                                table[i2, i3] = new PathSegment(table[i2, i1].Link, table[i2, i1].TotalLength + table[i1, i3].TotalLength, table[i2, i1].TotalTime + table[i1, i3].TotalTime);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < f_count; i++)
            {
                for (int j = 0; j < f_count; j++)
                {
                    Link link = null;
                    foreach (var x in facilitiesSpan[i].Links)
                    {
                        if (x.Unconnected && x.To == facilitiesSpan[j])
                        {
                            link = x;
                            break;
                        }
                    }

                    if (link != null)
                    {
                        if (table[i, j] == null || table[i, j].TotalTime > link.Time)
                        {
                            table[i, j] = new PathSegment(link, link.Length, link.Time);
                        }
                    }
                }
            }

            RouteTable result = new RouteTable();


            for (int i1 = 0; i1 < f_count; i1++)
            {
                for (int i2 = 0; i2 < f_count; i2++)
                {
                    if (table[i1, i2] != null)
                    {
                        result.Add((facilitiesSpan[i1], facilitiesSpan[i2]), table[i1, i2]);
                    }
                }
            }

            result.Setup();

            return result;
        }

        public IEnumerator<KeyValuePair<string, Facility>> GetEnumerator()
        {
            return facilities.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) facilities).GetEnumerator();
        }

        public void Add(KeyValuePair<string, Facility> item)
        {
            (facilities as IDictionary<string, Facility>).Add(item);
        }

        public void Clear()
        {
            facilities.Clear();
        }

        public bool Contains(KeyValuePair<string, Facility> item)
        {
            return (facilities as IDictionary<string, Facility>).Contains(item);
        }

        public void CopyTo(KeyValuePair<string, Facility>[] array, int arrayIndex)
        {
            (facilities as IDictionary<string, Facility>).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, Facility> item)
        {
            return (facilities as IDictionary<string, Facility>).Remove(item);
        }

        public int Count => facilities_list.Count;

        public bool IsReadOnly => (facilities as IDictionary<string, Facility>).IsReadOnly;

        public void Add(string key, Facility value)
        {
            facilities.Add(key, value);
            facilities_list.Add(value);
            value.Id = FacilityIdOffset + facilities_list.Count - 1;
        }

        public bool ContainsKey(string key)
        {
            return facilities.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            var facility = this[key];
            foreach (Facility v in facilities.Values)
            {
                v.Links.RemoveAll(x => x.To == facility || x.From == facility);
            }

            return facilities_list.Remove(facility) & facilities.Remove(key);
        }

        public void Remove(int key)
        {
            var facility = this[key];
            foreach (Facility v in facilities.Values)
            {
                v.Links.RemoveAll(x => x.To == facility || x.From == facility);
            }

            facilities.Remove(facility.Name);
            facilities_list.RemoveAt(key);
        }

        public bool TryGetValue(string key, out Facility value)
        {
            return facilities.TryGetValue(key, out value);
        }

        public Facility this[string key]
        {
            get => facilities[key];
            set
            {
                if (facilities.ContainsKey(key))
                {
                    int index = facilities_list.IndexOf(facilities[key]);
                    facilities_list[index] = value;
                }

                facilities[key] = value;
            }
        }

        public Facility this[int index]
        {
            get => facilities_list[index];
        }

        public ICollection<string> Keys => ((IDictionary<string,Facility>) facilities).Keys;

        public ICollection<Facility> Values => ((IDictionary<string,Facility>) facilities).Values;

        public Span<Facility> Span => CollectionsMarshal.AsSpan(facilities_list);

        public void AddRange(IEnumerable<Facility> list)
        {
            foreach (var facility in list)
            {
                Add(facility);
            }
        }
    }
}
