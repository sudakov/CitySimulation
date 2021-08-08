using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Entities;
using CitySimulation.Entity;
using CitySimulation.Tools;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation
{
    public class Appointment
    {
        public Facility Facility;
        public LogCityTime Time;
        public int Duration;
        public Range TimeRange;

        public Appointment(Facility facility, LogCityTime time, int duration)
        {
            Facility = facility;
            Time = time;
            Duration = duration;
            TimeRange = (time.TotalMinutes, time.TotalMinutes + duration);
        }

        public override string ToString()
        {
            return Facility.Name + ": " + Time;
        }
    }
}
