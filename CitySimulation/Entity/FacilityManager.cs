using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Navigation;
using CitySimulation.Tools;

namespace CitySimulation.Entity
{
    public class FacilityManager : IDictionary<string, Facility>
    {
        private Dictionary<string, Facility> facilities = new Dictionary<string, Facility>();
        private List<Facility> facilities_list = new List<Facility>();

        public void Add(Facility item)
        {
            Add(item.Name, item);
        }

        public void Link(string f1, string f2, double length)
        {
            Link(this[f1], this[f2], length);
        }

        public void Link(string f1, string f2)
        {
            Link(this[f1], this[f2]);
        }

        public void Link(Facility f1, Facility f2, double length)
        {
            f1.Links.Add(new Link(f1,f2, length));
            f2.Links.Add(new Link(f2,f1, length));
        }

        public void Link(Facility f1, Facility f2)
        {
            Link(f1, f2, Point.Distance(f1.Coords, f2.Coords));
        }

        public RouteTable CreateRouteTable()
        {
            RouteTable table = new RouteTable();

            foreach (Facility facility in facilities.Values)
            {
                foreach (Link link in facility.Links)
                {
                    table.Add((facility, link.To), new PathSegment(link, link.Length));
                }
            }

            foreach (Facility f1 in facilities.Values)
            {
                foreach (Facility f2 in facilities.Values)
                {
                    foreach (Facility f3 in facilities.Values)
                    {
                        if (f2 != f3)
                        {
                            if (table.ContainsKey((f2, f1)) && table.ContainsKey((f1, f3)) && (!table.ContainsKey((f2, f3)) || table[(f2, f3)].TotalLength > table[(f2, f1)].TotalLength + table[(f1, f3)].TotalLength))
                            {
                                table[(f2, f3)] = new PathSegment(table[(f2, f1)].Link, table[(f2, f1)].TotalLength + table[(f1, f3)].TotalLength);
                            }
                        }
                    }
                }
            }

            return table;
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
        }

        public bool ContainsKey(string key)
        {
            return facilities.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return facilities_list.Remove(facilities[key]) & facilities.Remove(key);
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

        public void AddRange(IEnumerable<Facility> list)
        {
            foreach (var facility in list)
            {
                Add(facility);
            }
        }
    }
}
