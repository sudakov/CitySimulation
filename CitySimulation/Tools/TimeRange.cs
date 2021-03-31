using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Tools
{
    public struct TimeRange
    {
        public int Start;
        public int End;
        public bool Reverse;

        public TimeRange(int start, int end)
        {
            Start = start;
            End = end;
            Reverse = Start > End;
        }

        public static implicit operator TimeRange((int, int) v)
        {
            return new TimeRange(v.Item1, v.Item2);
        }
    }
}
