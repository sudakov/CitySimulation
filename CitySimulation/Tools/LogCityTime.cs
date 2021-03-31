using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Tools
{
    public struct LogCityTime
    {
        public int Minutes { get; set; }
        public int Day { get; set; }

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
    }
}
