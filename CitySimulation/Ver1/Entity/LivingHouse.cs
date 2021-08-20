using CitySimulation.Entities;

namespace CitySimulation.Ver1.Entity
{
    public class LivingHouse : Facility
    {
        public int FamiliesCount { get; set; }
        public LivingHouse(string name) : base(name)
        {
        }
    }
}
