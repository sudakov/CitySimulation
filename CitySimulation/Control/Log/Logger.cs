using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CitySimulation.Entity;
using CitySimulation.Tools;

namespace CitySimulation.Control
{
    public abstract class Logger
    {
        public int SessionId { get; protected set; } = -1;

        public abstract void LogPersonInFacilityTime(LogCityTime start, LogCityTime end, Facility facility, Person person);

        public abstract int? Start();

        public abstract void Stop();

        public virtual void PreProcess()
        {

        }

        public virtual void Process()
        {

        }

        public virtual void PostProcess()
        {

        }
    }
}
