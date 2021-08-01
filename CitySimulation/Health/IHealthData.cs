using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Health
{
    public interface IHealthData
    {
        HealthStatus HealthStatus { get; }
        bool Infected { get; }
        void Process();
        bool TryInfect();
    }
}
