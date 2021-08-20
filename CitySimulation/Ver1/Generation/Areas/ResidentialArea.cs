using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CitySimulation.Behaviour;
using CitySimulation.Entities;
using CitySimulation.Generation.Areas;
using CitySimulation.Generation.Models;
using CitySimulation.Tools;
using CitySimulation.Ver1.Entity;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Generation
{

    public class ResidentialArea : Area
    {
        public int FamiliesCount { get; set; }
        // public int FamiliesPerHouse { get; set; }

        public House[] Houses { get; set; }
        public int HouseSpace { get; set; }
        public double[] HousesDistribution { get; set; } = { 1 };

        public Range SchoolDistance { get; set; }
        public int FamiliesPerSchool { get; set; }

        public int HouseGroupSize { get; set; } = 200;
        public int HouseGroupDistance { get; set; } = 70;


        private List<Facility> _houses = new List<Facility>();

        private ServicesConfig _servicesConfig = null;


        public int[] GetHousesCount()
        {
            int[] housesCount = HousesDistribution.Number().Select(x => (int)Math.Ceiling(FamiliesCount * x.Item2 / Houses[x.Item1].FamiliesPerHouse)).ToArray();

            return housesCount;
        }

        public override List<Facility> Generate(ref Point startPos)
        {
            _houses.Clear();

            int[] livingHousesCount = GetHousesCount();

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


            if (_servicesConfig != null)
            {
                //Добавляем сервисы и магазины
                var services = _servicesConfig.GenerateServices(FamiliesCount, maxHouseSize);
                if (services.Count > livingHousesCount.Sum() / 5)
                {
                    Debug.WriteLine($"Слишком много зданий сервиса в {Name} ({services.Count} на {livingHousesCount.Sum()} жилых домов)");
                }

                _houses.AddRange(services);
            }
           

            _houses = _houses.Shuffle(Controller.Random).ToList();


            Point currentPos = new Point(startPos);
            Point lastGroupPoint = new Point(startPos);


            List<(Point, int)> schoolPoints = new List<(Point, int)>();
            int ySchoolOffset = Math.Min(AreaDepth / 6, SchoolDistance.Middle/2);
            int minSchoolCount = Math.Max(1, _houses.OfType<LivingHouse>().Sum(x => x.FamiliesCount) / FamiliesPerSchool);

            float schoolRand = 1;

            for (int i = 0; i < _houses.Count; i++)
            {
                if (currentPos.Y + maxHouseSize + HouseSpace >= startPos.Y + AreaDepth)
                {
                    currentPos.Y = startPos.Y;
                    lastGroupPoint.Y = startPos.Y;

                    currentPos.X += maxHouseSize + HouseSpace;

                    if (currentPos.X - lastGroupPoint.X > HouseGroupSize)
                    {
                        currentPos.X += HouseGroupDistance;
                        lastGroupPoint.X = currentPos.X;
                    }
                }

                {
                    //Строим школы

                    if (schoolPoints.All(x => Point.Distance(x.Item1, currentPos) > x.Item2)
                        && currentPos.Y - startPos.Y > ySchoolOffset
                        && (startPos.Y + AreaDepth - maxHouseSize - HouseSpace) - currentPos.Y > ySchoolOffset
                        && currentPos.X - startPos.X > SchoolDistance.Start)
                    {
                        _houses.Insert(i, new School(Name + "_School" + index++)
                        {
                            WorkTime = new Range(8*60, 17*60),
                            StudyTime = new Range(8*60, 16*60),
                            Size = new Point(maxHouseSize, maxHouseSize),
                        });
                        schoolPoints.Add((new Point(currentPos.X, currentPos.Y), Controller.Random.Next(SchoolDistance.Start, SchoolDistance.End)));
                    }
                    else
                    {
                        if (0 < i / (float)_houses.Count - (schoolRand + schoolPoints.Count) / (float)minSchoolCount)
                        {
                            _houses.Insert(i, new School(Name + "_School" + index++)
                            {
                                WorkTime = new Range(8 * 60, 15 * 60),
                                StudyTime = new Range(8 * 60, 16 * 60),
                                Size = new Point(maxHouseSize, maxHouseSize),
                            });
                            schoolPoints.Add((new Point(currentPos.X, currentPos.Y), Controller.Random.Next(SchoolDistance.Start, SchoolDistance.End)));
                            schoolRand = (float)(Controller.Random.NextDouble() + 1) * 2;
                        }
                    }
                }

                _houses[i].Coords = new Point(currentPos.X, currentPos.Y);

                currentPos.Y += maxHouseSize + HouseSpace;

                if (currentPos.Y < lastGroupPoint.Y)
                {
                    lastGroupPoint.Y = startPos.Y;
                }
                else if (currentPos.Y - lastGroupPoint.Y > HouseGroupSize)
                {
                    currentPos.Y += HouseGroupDistance;
                    lastGroupPoint.Y = currentPos.Y;
                }
            }
            

            startPos = new Point(currentPos.X + maxHouseSize + HouseSpace, startPos.Y);

            return _houses;
        }

        public List<Facility> GenerateWithServices(ref Point startPos, ServicesConfig servicesConfig)
        {
            this._servicesConfig = servicesConfig;

            return Generate(ref startPos);

            // this._servicesConfig = null;
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

            //Назначаем студентам образовательные учреждения
            var students = families.SelectMany(x=>x.Members).Where(x => x.Behaviour is IStudent);

            var schools = _houses.OfType<School>().ToList();

            foreach (Person student in students)
            {
                School school = schools.Where(x => x.StudentsAge.InRange(student.Age)).MinBy(x => Point.Distance(x.Coords, student.Home.Coords));
                (student.Behaviour as IStudent).SetStudyPlace(school);
            }

            //Установим кол-во рабочих мест обр. учреждений в зависимости от кол-ва обучающихся
            foreach (School school in schools)
            {
                school.WorkersCount = (int)(students.Count(x=>x.Behaviour is IStudent behaviour && behaviour.StudyPlace == school) * 0.15f);
            }
        }

        public override void SetWorkers(IEnumerable<Person> persons)
        {
            

            // var unemployed = persons.Where(x => x.Behaviour is IPersonWithWork w && w.WorkPlace == null).ToList();
            //
            //
            //
            // Dictionary<Service, int> map = _houses.OfType<Service>().ToDictionary(x => x, x => x.WorkersCount);
            //
            // List<Service> services = map.Keys.ToList();
            //
            // {
            //     foreach (Service service in services)
            //     {
            //         var local = unemployed.Where(x => Point.Distance(service.Coords, x.Home.Coords) < 1000).Take((int)(map[service] * _servicesConfig.LocalWorkersRatio)).ToList();
            //         local.ForEach(x => unemployed.Remove(x));
            //
            //         map[service] -= local.Count;
            //
            //         local.ForEach(x => (x.Behaviour as IPersonWithWork)?.SetWorkplace(service));
            //     }
            // }
            //
            // var stack = new Stack<Person>(unemployed);
            // while (stack.Any() && map.Any())
            // {
            //     services = services.OrderBy(x => Controller.Random.Next()).ToList();
            //
            //     for (int i = 0; i < services.Count; i++)
            //     {
            //         if (stack.Any() && map.ContainsKey(services[i]))
            //         {
            //             var behaviour = stack.Pop();
            //             (behaviour.Behaviour as IPersonWithWork).SetWorkplace(services[i]);
            //             if (map[services[i]]-- == 0)
            //             {
            //                 map.Remove(services[i]);
            //             }
            //         }
            //     }
            // }
        }


        public override void Clear()
        {
            _houses.Clear();
        }


    }

}
