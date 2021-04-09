using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Behaviour;
using CitySimulation.Entity;

namespace CitySimulation.Generation.Persons
{
    public class PersonBehaviourGenerator
    {
        public int ElderyAge { get; set; } = int.MaxValue;
        public void GenerateBehaviour(Person person)
        {
            if (person.Age >= 18)
            {
                if (person.Age < ElderyAge)
                {
                    person.Behaviour = new PunctualWorkerBehaviour();
                }
            }
        }
    }
}
