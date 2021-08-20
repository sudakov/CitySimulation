using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entities;
using CitySimulation.Tools;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Behaviour
{
    public class HomeStayingBehaviour : PersonBehaviour
    {
        public override void UpdateAction(Person person, in CityTime dateTime, in int deltaTime)
        {
            AssignAppointment(person, dateTime.Day, dateTime.Minutes);

            if (CurrentAppointment != null)
            {
                ProcessCurrentAppointment(person, dateTime, deltaTime);
            }
            else if(person.Location != person.Home)
            {
                Move(person, person.Home, deltaTime);
            }
        }

        public override bool AppointVisit(in Facility facility, in LogCityTime time, in int duration, in bool force = false)
        {
            var range = new Range(time.Day * 24 * 60 + time.Minutes - AppointmentInterval, time.Day * 24 * 60 + time.Minutes + duration + AppointmentInterval);

            if (_appoints.Count == 0 || _appoints.TrueForAll(x => x.TimeRange.Intersection(range) == 0))
            {
                _appoints.Add(new Appointment(facility, time, duration));
                return true;
            }

            return false;
        }
    }
}
