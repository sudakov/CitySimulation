using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Entities;

namespace CitySimulation
{
    public class City
    {
        public List<Person> Persons = new List<Person>();
        public FacilityManager Facilities = new FacilityManager();

        public T Get<T>(string name) where T : Facility
        {
            return Facilities[name] as T;
        }
    }
}
