using System.Collections.Generic;
using CitySimulation.Health;
using CitySimulation.Navigation;
using CitySimulation.Tools;

namespace CitySimulation.Entities
{
    public class Facility : EntityBase
    {
#if !FACILITIES_DONT_CONTAIN_PERSONS
        public ConcurrentDictionary<string,Person> Persons = new ConcurrentDictionary<string, Person>();

        public int PersonsCount => Persons.Count;

#else
        public int PersonsCount;// { get; private set; }
#endif

        public Point Size { get; set; }

        public List<Link> Links = new List<Link>();

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


            if (!Persons.TryRemove(person.Name, out Person p))
            {
                throw new Exception("Person not in facility");
            }
#endif
        }

    }
}
