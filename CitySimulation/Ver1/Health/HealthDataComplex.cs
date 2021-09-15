using System;

namespace CitySimulation.Health
{
    public class HealthDataComplex : IHealthData
    {
        public HealthStatus HealthStatus { get; set; }
        public int StatusTimer;

        public bool Infected => HealthStatus == HealthStatus.InfectedSpread || HealthStatus == HealthStatus.InfectedIncubation;

        private const int IncubTime = 60 * 24;
        private const int SpreadTime = 60 * 24;
        private const int ImmuneTime = 60 * 24;

        public void Process()
        {
            if (StatusTimer != 0)
            {
                StatusTimer -= Controller.Instance.DeltaTime;
                if (StatusTimer == 0)
                {
                    switch (HealthStatus)
                    {
                        case HealthStatus.InfectedIncubation:
                            HealthStatus = HealthStatus.InfectedSpread;
                            StatusTimer = (1 + DateTime.Now.Millisecond % 7) * SpreadTime;
                            break;
                        case HealthStatus.InfectedSpread:
                            HealthStatus = HealthStatus.Recovered;
                            StatusTimer = (1 + DateTime.Now.Millisecond % 7) * ImmuneTime;
                            break;
                        case HealthStatus.Recovered:
                            HealthStatus = HealthStatus.Susceptible;
                            break;
                    }
                }
            }
        }

        public bool TryInfect()
        {
            if (HealthStatus == HealthStatus.Susceptible)
            {
                HealthStatus = HealthStatus.InfectedIncubation;
                StatusTimer = (1 + DateTime.Now.Millisecond % 7) * IncubTime;
                return true;
            }

            return false;
        }
    }
}
