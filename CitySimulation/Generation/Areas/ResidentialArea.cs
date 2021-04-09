using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CitySimulation.Behaviour;
using CitySimulation.Entity;
using CitySimulation.Generation.Areas;
using CitySimulation.Tools;

namespace CitySimulation.Generation
{

    public class ResidentialArea : Area
    {
        public int FamiliesCount { get; set; }
        // public int FamiliesPerHouse { get; set; }
        public int AreaDepth { get; set; }

        public House[] Houses { get; set; }
        public int HouseSpace { get; set; }
        public double[] HousesDistribution { get; set; } = { 1 };

        private List<Facility> _houses = new List<Facility>();

        private int[] servicesCount = null;
        private ServicesGenerator _servicesGenerator = null;

        public int[] GetHousesCount()
        {
            int[] housesCount = HousesDistribution.Number().Select(x => (int)Math.Ceiling(FamiliesCount * x.Item2 / Houses[x.Item1].FamiliesPerHouse)).ToArray();

            return housesCount;
        }

        public override List<Facility> Generate(ref Point startPos)
        {
            _houses.Clear();

            int[] livingHousesCount = GetHousesCount();
            Point currentPos = new Point(startPos);

            if (servicesCount != null && servicesCount.Sum() > livingHousesCount.Sum()/5)
            {
                Debug.WriteLine($"Слишком много зданий сервиса в {Name} ({servicesCount.Sum()} на {livingHousesCount.Sum()} жилых домов)");
            }

            int index = 0;
            for (var i = 0; i < livingHousesCount.Length; i++)
            {
                for (int j = 0; j < livingHousesCount[i]; j++)
                {
                    _houses.Add(new LivingHouse(Name + index++)
                    {
                        FamiliesCount = Houses[i].FamiliesPerHouse,
                        Size = new Point(Houses[i].Size, Houses[i].Size)
                    });
                }
            }

            int maxHouseSize = Houses.Max(x => x.Size);

            for (int i = 0; i < servicesCount.Length; i++)
            {
                for (int j = 0; j < servicesCount[i]; j++)
                {
                    Service service = _servicesGenerator.Generate(Name + index++, i + 1);
                    service.Size = new Point(maxHouseSize, maxHouseSize);
                    _houses.Add(service);
                }
            }

            _houses = _houses.Shuffle(Controller.Random).ToList();


            for (int i = 0; i < _houses.Count; i++)
            {
                if (currentPos.Y + maxHouseSize + HouseSpace >= startPos.Y + AreaDepth)
                {
                    currentPos.Y = startPos.Y;
                    currentPos.X += maxHouseSize + HouseSpace;
                }

                _houses[i].Coords = new Point(currentPos.X, currentPos.Y);


                currentPos.Y += maxHouseSize + HouseSpace;
            }


            // int[] houses = new int[livingHousesCount.Sum() + Math.Clamp(servicesCount?.Sum() ?? 0, 0, livingHousesCount.Sum()/5)];
            //
            // if (servicesCount != null)
            // {
            //     int j = 0;
            //     for (int i = 0; i < houses.Length - livingHousesCount.Sum() && servicesCount.Sum() > 0; i++)
            //     {
            //         while (servicesCount[(i + j) % servicesCount.Length] == 0)
            //         {
            //             j++;
            //         }
            //
            //         servicesCount[(i + j) % servicesCount.Length]--;
            //         houses[i] = (i + j) % servicesCount.Length;
            //     }
            // }
            //
            // houses = houses.OrderBy(x => Controller.Random.Next()).ToArray();


            // for (int i = 0; i < houses.Length; i++)
            // {
            //     if (currentPos.Y + HouseSize + HouseSpace >= startPos.Y + AreaDepth)
            //     {
            //         currentPos.Y = startPos.Y;
            //         currentPos.X += HouseSize + HouseSpace;
            //     }
            //
            //     Facility facility;
            //
            //     if (houses[i] == 0)
            //     {
            //         facility = new LivingHouse(Name + "_" + i)
            //         {
            //             Coords = new Point(currentPos.X, currentPos.Y + Controller.Random.Next(-HouseSpace,HouseSpace)),
            //             Size = new Point(HouseSize, (int)(HouseSize * 0.7))
            //         };
            //     }
            //     else
            //     {
            //         facility = _servicesGenerator.Generate(Name + "_S" + i, 
            //             new Point(currentPos.X, currentPos.Y + Controller.Random.Next(-HouseSpace, HouseSpace)), 
            //             new Point(HouseSize, (int)(HouseSize * 0.7)),
            //             houses[i]);
            //     }
            //
            //     _houses.Add(facility);
            //
            //     currentPos.Y += HouseSize + HouseSpace;// + Controller.Random.Next(HouseSize + HouseSpace);
            // }

            startPos = new Point(currentPos.X + maxHouseSize + HouseSpace, startPos.Y);

            return _houses;
        }

        public List<Facility> GenerateWithServices(ref Point startPos, int[] servicesCount, ServicesGenerator servicesGenerator)
        {
            this._servicesGenerator = servicesGenerator;
            this.servicesCount = servicesCount;

            return Generate(ref startPos);

            this.servicesCount = null;
            this._servicesGenerator = null;
        }

        public void Populate(IEnumerable<Family> families)
        {
            List<Family> list = new List<Family>(families); 
            foreach (var house in _houses.OfType<LivingHouse>())
            {
                list.PopItems(house.FamiliesCount).ForEach(x =>
                {
                    foreach (Person member in x.Members)
                    {
                        member.Home = house;
                    }
                });
            }

            if (list.Any())
            {
                throw new Exception("Not enough houses");
            }
        }

        // public List<Person> GeneratePeople()
        // {
        //     List<Person> list = new List<Person>();
        //
        //     foreach (LivingHouse livingHouse in _houses.OfType<LivingHouse>())
        //     {
        //         for (int j = 0; j < FamiliesPerHouse; j++)
        //         {
        //             var family = PersonGenerator.GenerateFamily();
        //             foreach (var behaviour in family.Select(x => x.Behaviour).OfType<IPersonWithHome>())
        //             {
        //                 behaviour.Home = livingHouse;
        //             }
        //
        //             list.AddRange(family);
        //         }
        //     }
        //     
        //
        //     return list;
        // }

        public void SetWorkForUnemployed(IEnumerable<Person> persons)
        {
            var unemployed = new Stack<IPersonWithWork>(persons.Select(x => x.Behaviour).OfType<IPersonWithWork>().Where(x=>x.WorkPlace == null).ToList());


            Dictionary<Service, int> map = _houses.OfType<Service>().ToDictionary(x => x, x => x.WorkersCount);

            List<Service> services = map.Keys.ToList();

            while (unemployed.Any() && map.Any())
            {
                services = services.OrderBy(x => Controller.Random.Next()).ToList();

                for (int i = 0; i < services.Count; i++)
                {
                    if (unemployed.Any() && map.ContainsKey(services[i]))
                    {
                        var behaviour = unemployed.Pop();
                        behaviour.SetWorkplace(services[i]);
                        if (map[services[i]]-- == 0)
                        {
                            map.Remove(services[i]);
                        }

                    }
                }
            }
        }

        public override void Clear()
        {
            _houses.Clear();
        }


    }

}
