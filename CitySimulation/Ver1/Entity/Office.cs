using CitySimulation.Entities;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Ver1.Entity
{
    public class Office : Facility, IWorkplace
    {
        public Office(string name) : base(name)
        {
        }

        public Range WorkTime { get; set; }
        public int WorkersCount { get; set; }
    }
}
