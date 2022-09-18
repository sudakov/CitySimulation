using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entities;
using CitySimulation.Tools;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Behaviour
{
    public class RegularAttendBehaviour : PersonBehaviour
    {
        public bool[] WorkDays = new bool[7] { true, true, true, true, true, false, false };

        protected Facility attendPlace;
        protected Range attendTime = new Range(8 * 60, 17 * 60);

        private const int PositiveDeltaCountToChange = 5;

        protected int _correction;
        public int Tolerance = 5;
        public int MaxCorrection = 5 * 60;

        private int _positiveDeltaCounter = 0;

        public RegularAttendBehaviour()
        {
        }

        public override void Setup(Person person)
        {
            base.Setup(person);
            if (attendPlace != null)
            {
                int maxHomeToWorkTime = (int)person.Context.Routes[(person.Home, attendPlace)].TotalLength / Speed;

                _correction = Controller.Random.Next(-maxHomeToWorkTime, Tolerance);
            }
        }

        public override void UpdateAction(Person person, in CityTime dateTime, in int deltaTime)
        {
            int day = dateTime.Day;
            int minutes = dateTime.Seconds / 60;


            AssignAppointment(person, day, minutes);

            if (CurrentAppointment != null)
            {
                ProcessCurrentAppointment(person, dateTime, deltaTime);

                return;
            }

            bool shouldWork = false;

            if (WorkDays[day % 7])
            {
                if (!attendTime.Reverse)
                {
                    shouldWork = attendTime.End > minutes && (attendTime.Start + _correction) <= minutes;
                }
                else
                {
                    shouldWork = attendTime.End >= minutes || (attendTime.Start + _correction) <= minutes;
                }
            }


            if (shouldWork && attendPlace != null)
            {
                if (person.Location == attendPlace)
                {
                    if (!(person.CurrentAction is Working))
                    {
                        SetAction(person, StandardActions.Working);
                    }
                }
                else
                {
                    Move(person, attendPlace, deltaTime);
                }
            }
            else
            {
                if (person.Location == person.Home)
                {
                    if (!(person.CurrentAction is Resting))
                    {
                        SetAction(person, StandardActions.Resting);
                    }
                }
                else
                {
                    Move(person, person.Home, deltaTime);
                }
            }
        }

        public override EntityAction SetAction(Person person, EntityAction action)
        {
            if (action is Working && !(person.CurrentAction is Working))
            {
                //Если дело происходит в полуночь, будут проблемы
                int delta = attendTime.Start - person.Context.CurrentTime.Seconds / 60;
                if (Math.Abs(delta) > Tolerance)
                {
                    if (delta > 0)
                    {
                        _positiveDeltaCounter++;

                        if (_positiveDeltaCounter > PositiveDeltaCountToChange)
                        {
                            _correction = Math.Clamp(_correction + delta / 4, -MaxCorrection, 0);
                        }

                    }
                    else
                    {
                        _positiveDeltaCounter = 0;
                        _correction = Math.Clamp(_correction + (delta + delta / 2), -MaxCorrection, 0);
                    }
                }
            }
            return base.SetAction(person, action);
        }

        public override bool AppointVisit(in Facility facility, in LogCityTime time, in int duration, in bool force = false)
        {
            if (!WorkDays[time.Day % 7] || force)
            {
                var range = new Range(time.Day * 24 * 60 + time.Minutes - AppointmentInterval, time.Day * 24 * 60 + time.Minutes + duration + AppointmentInterval);
                if (_appoints.Count == 0 || _appoints.TrueForAll(x => x.TimeRange.Intersection(range) == 0))
                {
                    _appoints.Add(new Appointment(facility, time, duration));
                    return true;
                }
            }
            else if (time.Minutes < attendTime.Start || time.Minutes > attendTime.End)
            {
                var range = new Range(time.Day * 24 * 60 + time.Minutes - AppointmentInterval, time.Day * 24 * 60 + time.Minutes + duration + AppointmentInterval);
                if (attendTime.Intersection(range) == 0)
                {
                    if (_appoints.Count == 0 || _appoints.TrueForAll(x => x.TimeRange.Intersection(range) == 0))
                    {
                        _appoints.Add(new Appointment(facility, time, duration));
                        return true;
                    }
                }
            }


            return false;
        }

        public override int? GetFreeTime(int day, in Range range)
        {
            if (WorkDays[day % 7])
            {
                if (range.Start >= attendTime.Start || Controller.Random.Next(0, 2) == 0)
                {
                    return base.GetFreeTime(day, new Range(attendTime.End, range.End)) ?? base.GetFreeTime(day, new Range(range.Start, range.End));
                }
                else
                {
                    return base.GetFreeTime(day, new Range(range.Start, attendTime.Start)) ?? base.GetFreeTime(day, new Range(attendTime.End, range.End));
                }
            }
            else
            {
                return base.GetFreeTime(day, range);
            }
        }
    }
}
