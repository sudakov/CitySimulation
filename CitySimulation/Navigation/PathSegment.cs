using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Navigation
{
    public class PathSegment
    {
        public Link Link;
        public double TotalLength;
        public double TotalTime;

        public PathSegment(Link link, double totalLength, double totalTime)
        {
            Link = link;
            TotalLength = totalLength;
            TotalTime = totalTime;
        }

        public override string ToString()
        {
            return $"{Link.ToString()} <{TotalLength}>";
        }
    }
}
