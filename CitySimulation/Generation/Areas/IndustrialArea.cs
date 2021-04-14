using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CitySimulation.Behaviour;
using CitySimulation.Entity;
using CitySimulation.Tools;

namespace CitySimulation.Generation
{
    public class IndustrialArea : Area
    {
        public float WorkplacesRatio { get; set; }

        public struct OfficeConfig
        {
            public int WorkersCount { get; set; }
            public TimeRange WorkTime { get; set; }
        }
        public int HouseSize { get; set; }
        public int HouseSpace { get; set; }

        public OfficeConfig[] Offices { get; set; }
        public int AreaLength { get; set; }

        private List<Office> _offices = new List<Office>();

        public override List<Facility> Generate(ref Point startPos)
        {
            _offices.Clear();
            Point currentPos = new Point(startPos);

            int delta = (AreaLength - (Offices.Length + 1) * (HouseSize + HouseSpace)) / Offices.Length;
            currentPos.X += delta;

            for (int i = 0; i < Offices.Length; i++)
            {
                if (currentPos.X + HouseSize + HouseSpace >= startPos.X + AreaLength)
                {
                    currentPos.X = startPos.X;
                    currentPos.Y += HouseSize + HouseSpace;
                }

                _offices.Add(new Office(Name + "_" + i)
                {
                    Coords = new Point(currentPos),
                    Size = new Point(HouseSize, (int)(HouseSize * 0.7)),
                    WorkersCount = Offices[i].WorkersCount,
                    WorkTime = Offices[i].WorkTime
                });

                currentPos.X += HouseSize + HouseSpace + delta;
            }

            startPos = new Point(startPos.X + AreaLength, startPos.Y);

            return _offices.Cast<Facility>().ToList();
        }

        public override void SetWorkForUnemployed(IEnumerable<Person> persons)
        {
            var unemployed = new Stack<IPersonWithWork>(persons.Select(x => x.Behaviour).OfType<IPersonWithWork>().Where(x => x.WorkPlace == null).ToList());

            int toEmploy = (int)(persons.Count(x => x.Behaviour is IPersonWithWork) * WorkplacesRatio);


            Dictionary<Office, int> map = _offices.ToDictionary(x=>x, x=>x.WorkersCount);

            while (unemployed.Any() && map.Any() && toEmploy > 0)
            {
                for (int i = 0; i < _offices.Count; i++)
                {
                    if (unemployed.Any() && map.ContainsKey(_offices[i]) && toEmploy > 0)
                    {
                        var behaviour = unemployed.Pop();
                        behaviour.SetWorkplace(_offices[i]);
                        if (map[_offices[i]]-- == 0)
                        {
                            map.Remove(_offices[i]);
                        }

                        toEmploy--;
                    }
                }
            }

            if (toEmploy > 0)
            {
                Debug.WriteLine(Name + ": " + toEmploy + " not employed");
            }
        }

        public override void Clear()
        {
            _offices.Clear();
        }
    }
}
