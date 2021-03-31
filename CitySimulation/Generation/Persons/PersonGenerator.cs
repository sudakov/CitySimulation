using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Entity;

namespace CitySimulation.Generation.Persons
{
    public abstract class PersonGenerator
    {
        public static int GeneratorIndex = 0;
        public abstract List<Person> GenerateFamily();
    }
}
