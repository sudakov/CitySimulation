using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CitySimulation.Entities;
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

        /// <summary>
        /// Помимо обычного перемещения в локацию есть это, которое позволяет добавить персонажа единожды, чтобы расчёт заражения не вызывался повторно
        /// </summary>
        /// <param name="person"></param>
        public void AddPersonInf(Person person)
        {
            lock (newPersons)
            {
                newPersons.Add(person);
            }
        }

        public void RemovePersonInf(Person person)
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
                //Сортировка для удаления случайности, внесённой порядком выполнения потоков
                newPersons.Sort(EntityComparer.Instance);

                //Считаем заражённых
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

                //Вычисляем вероятность заражения

                double newInfProbability = 1 - Math.Pow(1 - InfectionProbability, oldInfCount + newInfCount);
                double oldInfProbability = 1 - Math.Pow(1 - InfectionProbability, newInfCount);

                int oldCount = persons.Count;
                int newCount = newPersons.Count;

                //Пробуем заразить людей

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
