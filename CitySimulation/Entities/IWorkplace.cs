using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Entities
{
    interface IWorkplace
    {
        Range WorkTime { get; }
        int WorkersCount { get; set; }

    }
}
