using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CitySimulation.Navigation;
using CitySimulation.Tools;

namespace CitySimulation.Entities
{
    public class Facility : EntityBase
    {
        public string Type;
        public double InfectionProbability;

#if !FACILITIES_DONT_CONTAIN_PERSONS
        public ConcurrentDictionary<string,Person> Persons = new ConcurrentDictionary<string, Person>();

        public int PersonsCount => Persons.Count;

#else
        public int PersonsCount;// { get; private set; }
#endif

        public FacilityBehaviour Behaviour;
        public Point Size { get; set; }
        public Point[] Polygon { get; set; }

        public List<Link> Links = new List<Link>();

        public Facility(string name) : base(name)
        {
            
        }

        public override void Setup()
        {
            Behaviour.Facility = this;
            base.Setup();
        }

#if FACILITIES_DONT_CONTAIN_PERSONS
        private object locker = new object();
#endif
        public void AddPerson(Person person)
        {
            Behaviour.OnPersonAdd(person);
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
            Behaviour.OnPersonRemove(person);

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

        public override void PostProcess()
        {
            Behaviour.ProcessInfection();
        }

        public virtual string ToLogString()
        {
            return $"{Id} ({Type})";
        }

        public override string ToString()
        {
            return ToLogString();
        }
    }
}
