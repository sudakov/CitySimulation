using System;
using System.Collections.Generic;
using System.Text;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Entity
{
    public class School : Service
    {
        public Range StudentsAge = new Range(2, 20);
        public School(string name) : base(name)
        {
        }
    }
}
