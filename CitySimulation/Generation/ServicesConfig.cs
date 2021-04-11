using System.Collections.Generic;
using System.Linq;
using CitySimulation.Entity;
using CitySimulation.Tools;

namespace CitySimulation.Generation.Models
{
    public class ServicesConfig
    {
        public int FamiliesPerService { get; set; }
        public int FamiliesPerStore { get; set; }
        public Range WorkersPerService { get; set; } 
        public Range WorkersPerStore { get; set; }

        public float LocalWorkersRatio { get; set; }

        public List<Service> GenerateServices(int familiesCount, int size)
        {
            List<Service> res = new List<Service>();

            int servicesCount = familiesCount / FamiliesPerService;

            for (int i = 0; i < servicesCount; i++)
            {
                res.Add(new Service("MinService" + Service.GetId())
                {
                    WorkersCount = WorkersPerService.Random(Controller.Random),
                    WorkTime = new TimeRange(8 * 60, 18 * 60),
                    Size = new Point(size, size)
                });
            }


            int storesCount = familiesCount / FamiliesPerStore;

            for (int i = 0; i < storesCount; i++)
            {
                res.Add(new Service("Store" + Service.GetId())
                {
                    WorkersCount = WorkersPerStore.Random(Controller.Random),
                    WorkTime = new TimeRange(8 * 60, 18 * 60),
                    Size = new Point(size, size)
                });
            }

            return res;
        }
    }
}