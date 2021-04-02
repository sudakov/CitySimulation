using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CitySimulation.Behaviour;
using CitySimulation.Entity;
using CitySimulation.Generation.Persons;
using CitySimulation.Tools;

namespace CitySimulation.Generation
{

    public class ResidentialArea : Area
    {
        public int FamiliesCount { get; set; }
        public int FamiliesPerHouse { get; set; }
        public int AreaLength { get; set; }

        public int HouseSize { get; set; }
        public int HouseSpace { get; set; }

        public PersonGenerator PersonGenerator { get; set; }


        private List<Facility> _houses = new List<Facility>();

        private int[] servicesCount = null;
        private ServicesGenerator _servicesGenerator = null;

        public int GetHousesCount()
        {
            return (int)Math.Ceiling(FamiliesCount / (float)FamiliesPerHouse);
        }

        public override List<Facility> Generate(ref Point startPos)
        {
            _houses.Clear();

            int livingHousesCount = GetHousesCount();
            Point currentPos = new Point(startPos);

            if (servicesCount != null && servicesCount.Sum() > livingHousesCount/5)
            {
                Debug.WriteLine($"Слишком много зданий сервиса в {Name} ({servicesCount.Sum()} на {livingHousesCount} жилых домов)");
            }

            int[] houses = new int[livingHousesCount + Math.Clamp(servicesCount?.Sum() ?? 0, 0, livingHousesCount/5)];

            if (servicesCount != null)
            {
                int j = 0;
                for (int i = 0; i < houses.Length - livingHousesCount && servicesCount.Sum() > 0; i++)
                {
                    while (servicesCount[(i + j) % servicesCount.Length] == 0)
                    {
                        j++;
                    }

                    servicesCount[(i + j) % servicesCount.Length]--;
                    houses[i] = (i + j) % servicesCount.Length;
                }
            }

            houses = houses.OrderBy(x => Controller.Random.Next()).ToArray();


            for (int i = 0; i < houses.Length; i++)
            {
                if (currentPos.X + HouseSize + HouseSpace >= startPos.X + AreaLength)
                {
                    currentPos.X = startPos.X;
                    currentPos.Y += HouseSize + HouseSpace;
                }

                Facility facility;

                if (houses[i] == 0)
                {
                    facility = new LivingHouse(Name + "_" + i)
                    {
                        Coords = new Point(currentPos.X, currentPos.Y + Controller.Random.Next(-HouseSpace,HouseSpace)),
                        Size = new Point(HouseSize, (int)(HouseSize * 0.7))
                    };
                }
                else
                {
                    facility = _servicesGenerator.Generate(Name + "_S" + i, 
                        new Point(currentPos.X, currentPos.Y + Controller.Random.Next(-HouseSpace, HouseSpace)), 
                        new Point(HouseSize, (int)(HouseSize * 0.7)),
                        houses[i]);

                    // facility = new Service(Name + "_S" + i)
                    // {
                    //     Coords = new Point(currentPos),
                    //     Size = new Point(HouseSize, (int)(HouseSize * 0.7)),
                    //     WorkersCount = houses[i]
                    // };
                }

                _houses.Add(facility);

                currentPos.X += HouseSize + HouseSpace + Controller.Random.Next(HouseSize + HouseSpace);
            }

            startPos = new Point(startPos.X + AreaLength, startPos.Y);

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

        public List<Person> GeneratePeople()
        {
            List<Person> list = new List<Person>();

            foreach (LivingHouse livingHouse in _houses.OfType<LivingHouse>())
            {
                for (int j = 0; j < FamiliesPerHouse; j++)
                {
                    var family = PersonGenerator.GenerateFamily();
                    foreach (var behaviour in family.Select(x => x.Behaviour).OfType<IPersonWithHome>())
                    {
                        behaviour.Home = livingHouse;
                    }

                    list.AddRange(family);
                }
            }
            

            return list;
        }

        public void SetWorkForUnemployed(List<Person> persons)
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
