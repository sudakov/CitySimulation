﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entities;
using CitySimulation.Generation.Model2;
using CitySimulation.Tools;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Ver2.Entity.Behaviour
{
    public class ConfigurableBehaviour : PersonBehaviour
    {
        public Dictionary<string, FacilityConfigurable> PersistentFacilities = new ();
        public List<(LinkLocPeopleType type, List<FacilityConfigurable> facilities)> AvailableLocations = new ();

        private List<(FacilityConfigurable facility, Range timeRange, LinkLocPeopleType type)> _locationsForDay = new ();
        private List<(FacilityConfigurable facility, Range timeRange, LinkLocPeopleType type)> _currentFacilities = new ();

        private int _currentDay = -2;
        private int _prevDayContactsCount = 0;
        private int _todaysContactsCount = 0;
        private int _locationEnterTime = 0;

        private Dictionary<string, float> _minutesInLocation = new ();
        public Dictionary<string, float> MinutesInLocation { get; } = new ();
        public string Type { get; set; }
        public Dictionary<string, long> Money { get; } = new ();
        public Dictionary<string, List<(long money, string comment)>> IncomeHistory { get; } = new ();
        
        public int GetDayContactsCount()
        {
            return _prevDayContactsCount;
        }

        public void AddContactsCount(int count)
        {
            _todaysContactsCount += count;
        }

        public override void Setup(Person person)
        {
            base.Setup(person);
            _locationsForDay.Clear();
            _minutesInLocation.Clear();
            MinutesInLocation.Clear();
            Money.Clear();
            IncomeHistory.Clear();

            foreach (var (linkLocPeopleType, facilityConfigurables) in CollectionsMarshal.AsSpan(AvailableLocations))
            {
                _minutesInLocation.TryAdd(linkLocPeopleType.LocationType, 0);
                MinutesInLocation.TryAdd(linkLocPeopleType.LocationType, 0);

                foreach (Income income in linkLocPeopleType.Income)
                {
                    Money.TryAdd(income.Item, 0);
                    IncomeHistory.TryAdd(income.Item, new List<(long, string)>());
                }
            }

            Prewarm(person);
        }

        private void Prewarm(Person person)
        {
            AssignTodaysLocations(person, new CityTime(0, -1));
            var (facility, timeRange, type) = _locationsForDay.FirstOrDefault(x => x.timeRange.InRange(CityTime.SECONDS_IN_DAY));
            if (facility != null)
            {
                _currentFacilities.Add((facility, timeRange, type));
            }

            _locationsForDay.Clear();
        }

        public override void UpdateAction(Person person, in CityTime dateTime, in int deltaTime)
        {
            int sec = dateTime.Seconds;

            bool removeFlag = false;
            foreach (var current in CollectionsMarshal.AsSpan(_currentFacilities))
            {
                if (current.Item2.End <= sec)
                {
                    removeFlag = true;
                    break;
                }
            }

            if (removeFlag)
            {
                for (int i = _currentFacilities.Count - 1; i >= 0; i--)
                {
                    if (_currentFacilities[i].Item2.End <= sec)
                    {
                        // currentFacilities[i].Item1.RemovePersonInf(person);
                        _currentFacilities.RemoveAt(i);
                    }
                }
            }


            if (_currentDay != dateTime.Day)
            {
                _currentDay = dateTime.Day;
                _prevDayContactsCount = _todaysContactsCount;
                _todaysContactsCount = 0;

                var keys = MinutesInLocation.Keys.ToArray();

                foreach (var key in keys)
                {
                    MinutesInLocation[key] = _minutesInLocation[key];
                    _minutesInLocation[key] = 0;
                }

                AssignTodaysLocations(person, dateTime);
            }

            for (int i = _locationsForDay.Count - 1; i >= 0; i--)
            {
                if (_locationsForDay[i].timeRange.Start < sec)
                {
                    var (facility, timeRange, type) = _locationsForDay[i];

                    _minutesInLocation[facility.Type] += timeRange.Length;

                    _currentFacilities.Add((facility, timeRange, type));
                    // tuple.Item1.AddPersonInf(person);
                    _locationsForDay.RemoveAt(i);

                    //Rate per Day
                    foreach (Income income in type.Income.Where(x => x.Rate == Income.RatePerDay))
                    {
                        AddMoney(income.Item, income.Summ, $"{Income.RatePerDay} at {facility.Id} ({facility.Type})");
                    }
                }
            }
            
            if (_currentFacilities.Count != 0)
            {
                var (queueTopFacility, _, queueTopLocPeopleType) = _currentFacilities[^1];
                if (person.Location != queueTopFacility && !(person.CurrentAction is Moving moving && moving.Destination == queueTopFacility))
                {
                    //Rate per Fact
                    foreach (Income income in queueTopLocPeopleType.Income.Where(x=>x.Rate == Income.RatePerFact))
                    {
                        AddMoney(income.Item, income.Summ, $"{Income.RatePerFact} at {queueTopFacility.Id} ({queueTopFacility.Type})");
                    }

                    //Rate per Minute
                    if (_currentFacilities.Count > 1)
                    {
                        int locationDeltaTime = dateTime.TotalMinutes - _locationEnterTime;
                        (FacilityConfigurable, Range, LinkLocPeopleType) currentFacility = _currentFacilities[^2];

                        foreach (Income income in currentFacility.Item3.Income.Where(x => x.Rate == Income.RatePerMinute))
                        {
                            AddMoney(income.Item, income.Summ * locationDeltaTime, $"{Income.RatePerMinute} at {currentFacility.Item1.Id} ({currentFacility.Item1.Type})");
                        }
                        foreach (Income income in currentFacility.Item3.Income.Where(x => x.Rate == Income.RatePerHour))
                        {
                            AddMoney(income.Item, income.Summ * locationDeltaTime / 60, $"{Income.RatePerHour} at {currentFacility.Item1.Id} ({currentFacility.Item1.Type})");
                        }
                    }

                    _locationEnterTime = dateTime.TotalMinutes;
                    StartMoving(person, queueTopFacility, deltaTime);
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

        protected virtual void StartMoving(Person person, Facility facility, in int deltaTime)
        {
            //Teleporting
            person.SetLocation(facility);
        }

        public List<FacilityConfigurable> GetCurrentFacilities()
        {
            return _currentFacilities.Select(x => x.Item1).ToList();
        }

        /// <summary>
        /// Выбор мест для посещения
        /// </summary>
        /// <param name="person"></param>
        /// <param name="dateTime"></param>
        protected void AssignTodaysLocations(Person person, in CityTime dateTime)
        {
#if DEBUG
            if (_locationsForDay.Any())
            {
                throw new Exception("Planed location was not visited");
            }
#else
            _locationsForDay.Clear();
#endif

            var random = person.Context.Random;
            var availableLocations = AvailableLocations.Where(x => x.type.HealthStatus == null || x.type.HealthStatus.Contains(person.HealthData.HealthStatus)).Shuffle(random).ToList();
            foreach (var locationList in CollectionsMarshal.AsSpan(availableLocations))
            {
                double r;
                if (dateTime.Day % 7 < 5)
                {
                    //workday
                    r = locationList.type.WorkdaysMean / 5;
                }
                else
                {
                    //holiday
                    r = locationList.type.HolidayMean / 2;
                }

                if (random.RollBinary(r))
                {
                    FacilityConfigurable facility;
                    if (locationList.type.Ispermanent != 0)
                    {
                        //Если локация постоянная, берём её из словаря
                        facility = PersistentFacilities.GetValueOrDefault(locationList.type.LocationType, null);
                        if (facility == null)
                        {
                            facility = locationList.facilities.GetRandom(random);
                            PersistentFacilities.Add(locationList.type.LocationType, facility);
                        }
                    }
                    else
                    {
                        facility = locationList.facilities.GetRandom(random);
                    }

                    double start;
                    double end;

                    //Посещение места должно начаться строго сегодня
                    do
                    {
                        start = Math.Min(random.RollUniform(locationList.type.StartMean, locationList.type.StartStd), 23.5f);
                        end = start + Math.Max(random.RollUniform(locationList.type.DurationMean, locationList.type.DurationStd), 0.5f);
#if DEBUG
                        if (start > 23.5f || start >= end)
                        {
                            Debug.WriteLine(dateTime + ": bad random");
                        }
#endif
                    }
                    while (start > 23.5f || start >= end);


                    _locationsForDay.Add((facility, new Range((int)(start * 60 * 60), (int)(end * 60 * 60)), locationList.type));
                }
            }

            //Перевод времени текущих локации на день вперёд
            _currentFacilities = _currentFacilities.ConvertAll(x => (x.facility, x.timeRange - 24 * 60 * 60, x.type));
        }

        protected void AddMoney(string type, int money, string comment)
        {
            Money[type] += money;
            IncomeHistory[type].Add((money, comment));
        }
    }
}
