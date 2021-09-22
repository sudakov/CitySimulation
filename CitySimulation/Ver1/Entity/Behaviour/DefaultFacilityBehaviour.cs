using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Entities;
using CitySimulation.Health;

namespace CitySimulation.Ver1.Entity.Behaviour
{
    public class DefaultFacilityBehaviour : FacilityBehaviour
    {
        public HashSet<Person> Infectors = new HashSet<Person>();

        public override void OnPersonAdd(Person person)
        {
            if (person.HealthData.HealthStatus == HealthStatus.InfectedSpread)
            {
                lock (Infectors)
                {
                    Infectors.Add(person);
                }
            }

        }

        public override void OnPersonRemove(Person person)
        {
            lock (Infectors)
            {
                Infectors.Remove(person);
            }
        }
    }
}
