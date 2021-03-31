using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Navigation
{
    public class PathSegment
    {
        public Link Link;
        public double TotalLength;

        public PathSegment(Link link, double totalLength)
        {
            Link = link;
            TotalLength = totalLength;
        }

        public override string ToString()
        {
            return $"{Link.ToString()} <{TotalLength}>";
        }
    }
}
