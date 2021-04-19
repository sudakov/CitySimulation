using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CitySimulation.Behaviour;
using CitySimulation.Entity;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Generation.Persons
{
    public class PersonBehaviourGenerator
    {
        public Range WorkerAgeRange { get; set; }
        public Range StudentAgeRange { get; set; }

        public void GenerateBehaviour(Person person)
        {
            if (WorkerAgeRange.InRange(person.Age) && !person.Family.Children.Contains(person) && !(person.Gender == Gender.Female && person.Family.Children.Any(x=>x.Age < 3)))
            {
                person.Behaviour = new PunctualWorkerBehaviour();
            }

            if (StudentAgeRange.InRange(person.Age) && person.Family.Children.Contains(person))
            {
                person.Behaviour = new StudentBehaviour();
            }
        }
    }
}
