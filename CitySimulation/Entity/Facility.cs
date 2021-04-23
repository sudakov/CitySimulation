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

        public int InfectionPoints;
        public HashSet<Person> Infectors = new HashSet<Person>();
        public Facility(string name) : base(name)
        {
            
        }


#if FACILITIES_DONT_CONTAIN_PERSONS
        private object locker = new object();
#endif
        public void AddPerson(Person person)
        {
            if (person.HealthData.HealthStatus == HealthStatus.InfectedSpread)
            {
                lock (Infectors)
                {
                    Infectors.Add(person);
                }
            }

#if FACILITIES_DONT_CONTAIN_PERSONS
            lock (locker)
            {
                PersonsCount++;
                if (Name == "МФЦ" && PersonsCount > 500)
                {
                    var p = Controller.City.Persons.Where(x => x.Location == this).ToList();
                    int a = 3;
                }
            }
#else
            if (!Persons.TryAdd(person.Name, person))
            {
                throw new Exception("Person can't be added to facility");
            }
#endif
        }

        public void RemovePerson(Person person)
        {
            lock (Infectors)
            {
                Infectors.Remove(person);
            }

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
