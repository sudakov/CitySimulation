using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Tools
{
    public struct LogCityTime : IComparable<LogCityTime>
    {
        public int Minutes { get; set; }
        public int Day { get; set; }
        public int TotalMinutes => Day * 24 * 60 + Minutes;

        public LogCityTime(CityTime time)
        {
            Minutes = time.Minutes;
            Day = time.Day;
        }

        public LogCityTime(int day, int minutes)
        {
            Minutes = minutes;
            Day = day;
        }

        public override string ToString()
        {
            return $"Day: {Day}, {Minutes / 60}:{Minutes % 60}";
        }

        public int CompareTo(LogCityTime other)
        {
            var daysComparison = Day.CompareTo(other.Day);
            if (daysComparison != 0) return daysComparison;
            return Minutes.CompareTo(other.Minutes);
        }
    }
}
