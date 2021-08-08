using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CitySimulation.Behaviour;
using CitySimulation.Entity;
using CitySimulation.Tools;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Generation.Model2
{
    public class ConfigurableBehaviour : PersonBehaviour
    {
        public Dictionary<string, FacilityConfigurable> PersistentFacilities = new Dictionary<string, FacilityConfigurable>();
        public List<(LinkLocPeopleType, List<FacilityConfigurable>)> AvailableLocations = new List<(LinkLocPeopleType, List<FacilityConfigurable>)>();

        private List<(FacilityConfigurable, Range)> locationsForDay = new List<(FacilityConfigurable, Range)>();
        private List<(FacilityConfigurable, Range)> currentFacilities = new List<(FacilityConfigurable, Range)>();

        private int currentDay = -1;
        private int prevDayContactsCount = 0;
        private int todaysContactsCount = 0;

        public Dictionary<string, float> minutesInLocation = new Dictionary<string, float>();
        public string Type { get; set; }

        public int GetDayContactsCount()
        {
            return prevDayContactsCount;
        }

        public void AddContactsCount(int count)
        {
            todaysContactsCount += count;
        }

        public override void Setup(Person person)
        {
            base.Setup(person);
            foreach (var (linkLocPeopleType, facilityConfigurables) in AvailableLocations)
            {
                minutesInLocation.TryAdd(linkLocPeopleType.LocationType, 0);
            }
        }

        public override void UpdateAction(Person person, in CityTime dateTime, in int deltaTime)
        {
            int min = dateTime.Minutes;

            for (int i = currentFacilities.Count - 1; i >= 0; i--)
            {
                if (currentFacilities[i].Item2.End <= min)
                {
                    currentFacilities[i].Item1.RemovePersonInf(person);
                    currentFacilities.RemoveAt(i);
                }
            }

            if (currentDay != dateTime.Day)
            {
                currentDay = dateTime.Day;
                prevDayContactsCount = todaysContactsCount;
                todaysContactsCount = 0;
                AssignTodaysLocations(person, dateTime);
            }

            for (int i = locationsForDay.Count - 1; i >= 0; i--)
            {
                if (locationsForDay[i].Item2.Start < min)
                {
                    (FacilityConfigurable, Range) tuple = locationsForDay[i];
                    minutesInLocation[tuple.Item1.Type] += tuple.Item2.Length;

                    currentFacilities.Add(tuple);
                    tuple.Item1.AddPersonInf(person);
                    locationsForDay.RemoveAt(i);
                }
            }
            
            if (currentFacilities.Count != 0)
            {
                if (person.Location != currentFacilities[^1].Item1)
                {
                    person.SetLocation(currentFacilities[^1].Item1);
                }
            }
            else if(person.Location != null)
            {
                person.SetLocation(null);
            }
        }

        /// <summary>
        /// Выбор мест для посещения
        /// </summary>
        /// <param name="person"></param>
        /// <param name="dateTime"></param>
        protected void AssignTodaysLocations(Person person, in CityTime dateTime)
        {
#if DEBUG
            if (locationsForDay.Any())
            {
                throw new Exception("Planed location was not visited");
            }
#else
            locationsForDay.Clear();
#endif

            var random = person.Context.Random;

            foreach (var location in AvailableLocations.Shuffle(random))
            {
                double r;
                if (dateTime.Day % 7 < 5)
                {
                    //workday
                    r = location.Item1.WorkdaysMean / 5;
                }
                else
                {
                    //holiday
                    r = location.Item1.HolidayMean / 2;
                }

                if (random.RollBinary(r))
                {
                    FacilityConfigurable facility;
                    if (location.Item1.Ispermanent != 0)
                    {
                        //Если локация постоянная, берём её из словаря
                        facility = PersistentFacilities.GetValueOrDefault(location.Item1.LocationType, null);
                        if (facility == null)
                        {
                            facility = location.Item2.GetRandom(random);
                            PersistentFacilities.Add(location.Item1.LocationType, facility);
                        }
                    }
                    else
                    {
                        facility = location.Item2.GetRandom(random);
                    }

                    double start;
                    double end;

                    //Посещение место должно начаться строго сегодня
                    do
                    {
                        start = Math.Min(random.RollUniform(location.Item1.StartMean, location.Item1.StartStd), 23.5f);
                        end = start + Math.Max(random.RollUniform(location.Item1.DurationMean, location.Item1.DurationStd), 0.5f);
#if DEBUG
                        if (start > 23.5f || start >= end)
                        {
                            Debug.WriteLine(dateTime + ": bad random");
                        }
#endif
                    }
                    while (start > 23.5f || start >= end);


                    locationsForDay.Add((facility, new Range((int)(start * 60), (int)(end * 60))));
                }
            }

            //Перевод времени текущих локации на день вперёд
            currentFacilities = currentFacilities.ConvertAll(x => (x.Item1, x.Item2 - 24 * 60));
        }
    }
}
