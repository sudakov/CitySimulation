using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Ver1.Entity
{
    public class School : Service
    {
        public Range StudentsAge = new Range(2, 20);
        public Range StudyTime;
        public School(string name) : base(name)
        {
        }
    }
}
