using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CitySimulation.Control;
using CitySimulation.Navigation;
using CitySimulation.Tools;

namespace CitySimulation.Entity
{
    public class Facility : Entity
    {
#if !FACILITIES_DONT_CONTAIN_PERSONS
        public ConcurrentDictionary<string,Person> Persons = new ConcurrentDictionary<string, Person>();

        public int PersonsCount => Persons.Count;

#else
        public int PersonsCount;// { get; private set; }
#endif

        public Point Size { get; set; }

        public List<Link> Links = new List<Link>();

        public Facility(string name) : base(name)
        {
            
        }

#if FACILITIES_DONT_CONTAIN_PERSONS
        private object locker = new object();
#endif
        public void AddPerson(Person person)
        {
#if FACILITIES_DONT_CONTAIN_PERSONS
            lock (locker)
            {
                PersonsCount++;
            }
#else
            if (!Persons.TryAdd(person.Name, person))
            {
                throw new Exception("Person can't be added to facility");
            }
#endif
        }

        public void RemovePerson(string name)
        {
#if FACILITIES_DONT_CONTAIN_PERSONS
            lock (locker)
            {
                PersonsCount--;
            }
#else


            if (!Persons.TryRemove(name, out Person person))
            {
                throw new Exception("Person not in facility");
            }
#endif
        }

    }
}
