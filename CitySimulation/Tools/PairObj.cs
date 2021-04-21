using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Tools
{
    public class PairObj<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public PairObj(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
    }
}
