using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CitySimulation.Entity;
using CitySimulation.Tools;

namespace CitySimulation.Generation.Model2
{
    public class FacilityConfigurable : Facility
    {
        public string Type;
        public double InfectionProbability;

        private List<Person> newPersons = new List<Person>();
        private List<Person> persons = new List<Person>();

        public FacilityConfigurable(string name) : base(name)
        {

        }

        public void AddPerson(Person person)
        {
            lock (newPersons)
            {
                newPersons.Add(person);
            }
        }

        public void RemovePerson(Person person)
        {
            lock (newPersons)
            {
                persons.Remove(person);
            }
        }

        public override void PostProcess()
        {
            base.PostProcess();

            if (newPersons.Count != 0)
            {
                newPersons.Sort(EntityComparer.Instance);

                int newInfCount = 0;
                int oldInfCount = 0;

                foreach (var person in newPersons)
                {
                    if (person.HealthData.Infected)
                    {
                        newInfCount++;
                    }
                }

                foreach (var person in persons)
                {
                    if (person.HealthData.Infected)
                    {
                        oldInfCount++;
                    }
                }

                double newInfProbability = 1 - Math.Pow(1 - InfectionProbability, oldInfCount + newInfCount);
                double oldInfProbability = 1 - Math.Pow(1 - InfectionProbability, newInfCount);

                int oldCount = persons.Count;
                int newCount = newPersons.Count;

                
                foreach (var person in newPersons)
                {
                    if (newInfProbability != 0)
                    {
                        if (!person.HealthData.Infected && Context.Random.RollBinary(newInfProbability))
                        {
                            person.HealthData.TryInfect();
                        }
                    }

                    ((ConfigurableBehaviour) person.Behaviour).AddContactsCount(oldCount + newCount - 1);;
                }

                foreach (var person in persons)
                {
                    if (oldInfProbability != 0)
                    {
                        if (!person.HealthData.Infected && Context.Random.RollBinary(oldInfProbability))
                        {
                            person.HealthData.TryInfect();
                        }
                    }
                   
                    ((ConfigurableBehaviour) person.Behaviour).AddContactsCount(newCount);
                }
                

                persons.AddRange(newPersons);
                newPersons.Clear();
            }

        }
    }
}
