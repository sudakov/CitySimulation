using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CitySimulation.Behaviour;
using CitySimulation.Entity;
using CitySimulation.Generation.Models;
using CitySimulation.Tools;

namespace CitySimulation.Generation.Areas
{
    public class AdministrativeArea : Area
    {
        public Service[] Service { get; set; }

        public int AreaDepth { get; set; }
        public int HouseSpace { get; set; }

        public float WorkplacesRatio { get; set; }

        public override List<Facility> Generate(ref Point startPos)
        {
            Point currentPos = new Point(startPos);
            int maxHouseSize = Service.Max(x => Math.Max(x.Size.X, x.Size.Y));

            foreach (Service service in Service)
            {
                if (currentPos.Y + maxHouseSize + HouseSpace >= startPos.Y + AreaDepth)
                {
                    currentPos.Y = startPos.Y;
                    currentPos.X += maxHouseSize + HouseSpace;

                }
                service.Coords = new Point(currentPos.X, currentPos.Y);

                currentPos.Y += maxHouseSize + HouseSpace;
            }

            startPos = new Point(currentPos.X + maxHouseSize + HouseSpace, startPos.Y);

            return Service.Cast<Facility>().ToList();
        }

        public override void SetWorkers(IEnumerable<Person> persons)
        {
            var unemployed = persons.Select(x => x.Behaviour).OfType<IPersonWithWork>().Where(x => x.WorkPlace == null).ToList();

            int workplacesCount = (int)(persons.Count(x=>x.Behaviour is IPersonWithWork) * WorkplacesRatio);
            int workerPerPlace = workplacesCount / Service.Length;


            foreach (Service service in Service)
            {
                service.WorkersCount = workerPerPlace;
                var workers = unemployed.PopItems(workerPerPlace);
                workplacesCount -= workers.Count;
                workers.ForEach(x => x.SetWorkplace(service));
            }

            while (unemployed.Any() && workplacesCount > 0)
            {
                unemployed.PopItems(1).Single().SetWorkplace(Service.GetRandom(Controller.Random));
                workplacesCount--;
            }
        }
    }
}
