using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Entity
{
    public class LivingHouse : Facility
    {
        public int FamiliesCount { get; set; }
        public LivingHouse(string name) : base(name)
        {
        }
    }
}
