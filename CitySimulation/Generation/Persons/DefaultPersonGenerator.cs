using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Behaviour;
using CitySimulation.Entity;
using CitySimulation.Tools;

namespace CitySimulation.Generation.Persons
{
    public class DefaultPersonGenerator : PersonGenerator
    {
        public int WorkersPerFamily { get; set; }

        public override List<Person> GenerateFamily()
        {
            List<Person> list = new List<Person>();
            for (int i = 0; i < WorkersPerFamily; i++)
            {
                list.Add(new Person("p" + GeneratorIndex++){Behaviour = new PunctualWorkerBehaviour(null)});
            }

            return list;
        }
    }
}
