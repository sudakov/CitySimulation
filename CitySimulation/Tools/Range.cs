using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Tools
{
    public struct Range
    {
        public int Start;
        public int End;
        public bool Reverse;

        public Range(int start, int end)
        {
            Start = start;
            End = end;
            Reverse = Start > End;
        }

        public bool InRange(int value, bool includeLeft = true, bool includeRight = false)
        {
            return !Reverse
                ? ((includeLeft ? Start <= value : Start < value) && (includeRight ? End >= value : End > value))
                : ((includeLeft ? Start <= value : Start < value) || (includeRight ? End >= value : End > value));
        }

        public static implicit operator Range((int, int) v)
        {
            return new Range(v.Item1, v.Item2);
        }

        public override string ToString()
        {
            return Start + ".." + End;
        }
    }
}
