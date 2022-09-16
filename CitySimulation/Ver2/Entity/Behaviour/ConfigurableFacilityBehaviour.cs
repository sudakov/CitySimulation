using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using CitySimulation.Entities;
using CitySimulation.Health;
using CitySimulation.Tools;

namespace CitySimulation.Ver2.Entity.Behaviour
{
    public class ConfigurableFacilityBehaviour : FacilityBehaviour
    {
        private List<Person> newPersons = new List<Person>();
        private List<Person> persons = new List<Person>();

        public override void OnPersonAdd(Person person)
        {
            lock (newPersons)
            {
                newPersons.Add(person);
            }
        }

        public override void OnPersonRemove(Person person)
        {
            lock (newPersons)
            {
                newPersons.Remove(person);
                persons.Remove(person);
            }
        }

        public override void ProcessInfection()
        {
            if (newPersons.Count != 0)
            {
                //Сортировка для удаления случайности, внесённой порядком выполнения потоков
                newPersons.Sort(EntityComparer.Instance);

                //Считаем заражённых
                int newInfCount = 0;
                int oldInfCount = 0;

                foreach (var person in CollectionsMarshal.AsSpan(newPersons))
                {
                    if (person.HealthData.HealthStatus == HealthStatus.InfectedSpread)
                    {
                        newInfCount++;
                    }
                }

                foreach (var person in CollectionsMarshal.AsSpan(persons))
                {
                    if (person.HealthData.HealthStatus == HealthStatus.InfectedSpread)
                    {
                        oldInfCount++;
                    }
                }

                //Вычисляем вероятность заражения

                double newInfProbability = 1 - Math.Pow(1 - Facility.InfectionProbability, oldInfCount + newInfCount);
                double oldInfProbability = 1 - Math.Pow(1 - Facility.InfectionProbability, newInfCount);

                int oldCount = persons.Count;
                int newCount = newPersons.Count;

                //Пробуем заразить людей

                foreach (var person in CollectionsMarshal.AsSpan(newPersons))
                {
                    if (newInfProbability != 0)
                    {
                        if (person.HealthData.HealthStatus == HealthStatus.Susceptible && Facility.Context.Random.RollBinary(newInfProbability))
                        {
                            person.HealthData.TryInfect();
                        }
                    }

                    ((ConfigurableBehaviour)person.Behaviour).AddContactsCount(oldCount + newCount - 1); ;
                }

                foreach (var person in CollectionsMarshal.AsSpan(persons))
                {
                    if (oldInfProbability != 0)
                    {
                        if (person.HealthData.HealthStatus == HealthStatus.Susceptible && Facility.Context.Random.RollBinary(oldInfProbability))
                        {
                            person.HealthData.TryInfect();
                        }
                    }

                    ((ConfigurableBehaviour)person.Behaviour).AddContactsCount(newCount);
                }


                persons.AddRange(newPersons);
                newPersons.Clear();
            }
        }
    }
}
