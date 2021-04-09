using System.Linq;

namespace CitySimulation.Generation.Models
{
    public struct ServicesConfig
    {
        public int ServiceWorkersCount { get; set; }
        public int MaxWorkersPerService { get; set; }
        public ServicesGenerator ServicesGenerator { get; set; }

        public int[] ServicesCount()
        {
            int[] count = new int[MaxWorkersPerService];
            if (MaxWorkersPerService > 0)
            {
                int sum = (1 + MaxWorkersPerService) * MaxWorkersPerService / 2;

                for (int i = 1; i < MaxWorkersPerService; i++)
                {
                    count[i - 1] = (MaxWorkersPerService - i + 1) * ServiceWorkersCount / sum;
                }
                count[MaxWorkersPerService - 1] = ServiceWorkersCount - count.Sum();
            }
            return count;
        }
    }
}