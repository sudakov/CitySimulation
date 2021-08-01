using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace CitySimulation
{
    public abstract class AsyncModule
    {
        public abstract void RunAsync(Barrier barrier);
    }
}
