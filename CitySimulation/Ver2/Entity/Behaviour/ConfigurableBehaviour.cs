using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entities;
using CitySimulation.Generation.Model2;
using CitySimulation.Tools;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Ver2.Entity.Behaviour
{
    public class ConfigurableBehaviour : PersonBehaviour
    {
        public Dictionary<string, FacilityConfigurable> PersistentFacilities = new Dictionary<string, FacilityConfigurable>();
        public List<(LinkLocPeopleType, List<FacilityConfigurable>)> AvailableLocations = new List<(LinkLocPeopleType, List<FacilityConfigurable>)>();

        private List<(FacilityConfigurable, Range, LinkLocPeopleType)> locationsForDay = new List<(FacilityConfigurable, Range, LinkLocPeopleType)>();
        private List<(FacilityConfigurable, Range, LinkLocPeopleType)> currentFacilities = new List<(FacilityConfigurable, Range, LinkLocPeopleType)>();

        private int currentDay = -1;
        private int prevDayContactsCount = 0;
        private int todaysContactsCount = 0;
        private int locationEnterTime = 0;

        private Dictionary<string, float> minutesInLocation = new Dictionary<string, float>();
        public Dictionary<string, float> MinutesInLocation { get; private set; } = new Dictionary<string, float>();
        public string Type { get; set; }
        public Dictionary<string, long> Money { get; set; } = new Dictionary<string, long>();
        public Dictionary<string, List<(long, string)>> IncomeHistory { get; set; } = new Dictionary<string, List<(long, string)>>();

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
                MinutesInLocation.TryAdd(linkLocPeopleType.LocationType, 0);

                foreach (Income income in linkLocPeopleType.Income)
                {
                    Money.TryAdd(income.Item, 0);
                    IncomeHistory.TryAdd(income.Item, new List<(long, string)>());
                }
            }
        }

        public override void UpdateAction(Person person, in CityTime dateTime, in int deltaTime)
        {
            int min = dateTime.Minutes;

            for (int i = currentFacilities.Count - 1; i >= 0; i--)
            {
                if (currentFacilities[i].Item2.End <= min)
                {
                    // currentFacilities[i].Item1.RemovePersonInf(person);
                    currentFacilities.RemoveAt(i);
                }
            }

            if (currentDay != dateTime.Day)
            {
                currentDay = dateTime.Day;
                prevDayContactsCount = todaysContactsCount;
                todaysContactsCount = 0;

                var keys = MinutesInLocation.Keys.ToArray();

                foreach (var key in keys)
                {
                    MinutesInLocation[key] = minutesInLocation[key];
                    minutesInLocation[key] = 0;
                }

                AssignTodaysLocations(person, dateTime);
            }

            for (int i = locationsForDay.Count - 1; i >= 0; i--)
            {
                if (locationsForDay[i].Item2.Start < min)
                {
                    (FacilityConfigurable, Range, LinkLocPeopleType) tuple = locationsForDay[i];
                    minutesInLocation[tuple.Item1.Type] += tuple.Item2.Length;

                    currentFacilities.Add(tuple);
                    // tuple.Item1.AddPersonInf(person);
                    locationsForDay.RemoveAt(i);

                    //Rate per Day
                    foreach (Income income in tuple.Item3.Income.Where(x => x.Rate == Income.RatePerDay))
                    {
                        AddMoney(income.Item, income.Summ, $"{Income.RatePerDay} at {tuple.Item1.Id} ({tuple.Item1.Type})");
                    }
                }
            }
            
            if (currentFacilities.Count != 0)
            {
                var current = currentFacilities[^1];
                if (person.Location != current.Item1 && !(person.CurrentAction is Moving moving && moving.Destination == current.Item1))
                {
                    //Rate per Fact
                    foreach (Income income in current.Item3.Income.Where(x=>x.Rate == Income.RatePerFact))
                    {
                        AddMoney(income.Item, income.Summ, $"{Income.RatePerFact} at {current.Item1.Id} ({current.Item1.Type})");
                    }

                    //Rate per Minute
                    if (currentFacilities.Count > 1)
                    {
                        int locationDeltaTime = dateTime.TotalMinutes - locationEnterTime;
                        (FacilityConfigurable, Range, LinkLocPeopleType) currentFacility = currentFacilities[^2];

                        foreach (Income income in currentFacility.Item3.Income.Where(x => x.Rate == Income.RatePerMinute))
                        {
                            AddMoney(income.Item, income.Summ * locationDeltaTime, $"{Income.RatePerMinute} at {currentFacility.Item1.Id} ({currentFacility.Item1.Type})");
                        }
                        foreach (Income income in currentFacility.Item3.Income.Where(x => x.Rate == Income.RatePerHour))
                        {
                            AddMoney(income.Item, income.Summ * locationDeltaTime / 60, $"{Income.RatePerHour} at {currentFacility.Item1.Id} ({currentFacility.Item1.Type})");
                        }
                    }

                    locationEnterTime = dateTime.TotalMinutes;
                    StartMoving(person, current.Item1, deltaTime);
                }
            }
            else if(person.Location != null)
            {
                StartMoving(person,null, deltaTime);
            }

            if (person.CurrentAction is Moving moving2)
            {
                if (moving2.Destination == person.Location)
                {
                    person.CurrentAction = null;
                }
                else
                {
                    Move(person, moving2.Destination, deltaTime);
                }
            }
        }

        public List<FacilityConfigurable> GetCurrentFacilities()
        {
            return currentFacilities.Select(x => x.Item1).ToList();
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
            var availableLocations = AvailableLocations.Where(x => x.Item1.HealthStatus == null || x.Item1.HealthStatus.Contains(person.HealthData.HealthStatus)).Shuffle(random).ToList();
            foreach (var location in availableLocations)
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


                    locationsForDay.Add((facility, new Range((int)(start * 60), (int)(end * 60)), location.Item1));
                }
            }

            //Перевод времени текущих локации на день вперёд
            currentFacilities = currentFacilities.ConvertAll(x => (x.Item1, x.Item2 - 24 * 60, x.Item3));
        }

        protected void AddMoney(string type, int money, string comment)
        {
            Money[type] += money;
            IncomeHistory[type].Add((money, comment));
        }

        protected virtual void StartMoving(Person person, Facility facility, in int deltaTime)
        {
            person.SetLocation(facility);
        }
    }
}
