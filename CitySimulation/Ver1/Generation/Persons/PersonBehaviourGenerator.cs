using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CitySimulation.Behaviour;
using CitySimulation.Entities;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Generation.Persons
{
    public class PersonBehaviourGenerator
    {
        public AgesConfig AgesConfig { get; set; }

        public void GenerateBehaviour(Person person)
        {
            if (AgesConfig.WorkerAgeRange.InRange(person.Age) && !person.Family.Children.Contains(person) && !(person.Gender == Gender.Female && person.Family.Children.Any(x=>x.Age < 3)))
            {
                person.Behaviour = new WorkerBehaviour();
            }
            else if (AgesConfig.StudentAgeRange.InRange(person.Age) && person.Family.Children.Contains(person))
            {
                person.Behaviour = new StudentBehaviour();
            }
            else if(AgesConfig.WorkerAgeRange.InRange(person.Age) || AgesConfig.StudentAgeRange.InRange(person.Age))
            {
                person.Behaviour = new HomeStayingBehaviour();
            }
        }
    }
}
