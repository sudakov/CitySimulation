using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CitySimulation
{
    public class CityTime
    {
        public int Minutes;
        // public int Minutes {
        //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //     get; 
        //     set;
        // }

        public CityTime()
        {
        }

        public CityTime(CityTime cityTime)
        {
            Day = cityTime.Day;
            Minutes = cityTime.Minutes;
        }

        public int Day;
        // {
        //     // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //     get; 
        //     set;
        // }

        public int TotalMinutes => Day * 24 * 60 + Minutes;

        public void AddMinutes(int value)
        {
            Minutes += value;

            if (Minutes >= 24 * 60)
            {
                Minutes %= (24 * 60);
                Day++;
            }
        }

        public int DayOfWeek()
        {
            return Day % 7 + 1;
        }

        public override string ToString()
        {
            return $"Day: {Day}, {MinutesToTime(Minutes)}";
        }

        public static string MinutesToTime(int minutes)
        {
            return $"{(minutes / 60):00}:{(minutes % 60):00}";
        }

        public (int, int) ToTuple()
        {
            return (Day, Minutes);
        }
    }
}
