using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CitySimulation
{
    public class CityTime
    {
        public const int SECONDS_IN_DAY = 24 * 60 * 60;
        public const uint U_SECONDS_IN_DAY = 24 * 60 * 60;

        public int Seconds;
        public int Day;

        public CityTime()
        {
        }

        public CityTime(int seconds, int day)
        {
            Seconds = seconds;
            Day = day;
        }

        public CityTime(CityTime cityTime)
        {
            Day = cityTime.Day;
            Seconds = cityTime.Seconds;
        }

        public int TotalMinutes => Day * 24 * 60 + Seconds / 60;
        public ulong TotalSeconds => (ulong)Day * U_SECONDS_IN_DAY + (ulong)Seconds;

        public void AddSeconds(int value)
        {
            Seconds += value;

            if (Seconds >= SECONDS_IN_DAY)
            {
                Seconds %= SECONDS_IN_DAY;
                Day++;
            }
        }

        // public void AddMinutes(int value)
        // {
        //     Minutes += value;
        //
        //     if (Minutes >= 24 * 60)
        //     {
        //         Minutes %= (24 * 60);
        //         Day++;
        //     }
        // }

        public int DayOfWeek()
        {
            return Day % 7 + 1;
        }

        public override string ToString()
        {
            return $"{Day}, {MinutesToTime(Seconds)}";
        }

        public static string MinutesToTime(int seconds)
        {
            return $"{(seconds / (60*60)):00}:{((seconds / 60) % 60):00}:{(seconds % 60):00}";
        }

        public (int, int) ToTuple()
        {
            return (Day, Seconds);
        }
    }
}
