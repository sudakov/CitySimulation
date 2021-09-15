using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Control;
using CitySimulation.Entities;
using CitySimulation.Tools;
using CitySimulation.Ver2.Control;
using CitySimulation.Ver2.Entity.Behaviour;

namespace CitySimulation.Health
{
    public class HealthDataSimple : IHealthData
    {
        public Person person;

        private HealthStatus healthStatus;

        public HealthStatus HealthStatus
        {
            get => healthStatus;
            set
            {
                healthStatus = value;
                if (value == HealthStatus.InfectedSpread)
                {
                    Infected = true;
                }
            }
        }

        public bool Infected { get; private set; }

        private int currentDay = -1;
        private int? stateChangeMinutes = null;

        public HealthDataSimple(Person person)
        {
            this.person = person;
        }

        public void Process()
        {
            if (stateChangeMinutes != null)
            {
                int currentMinutes = person.Context.CurrentTime.TotalMinutes;
                if (currentMinutes > stateChangeMinutes)
                {
                    stateChangeMinutes = null;
                    var param = ((ConfigParamsSimple)person.Context.Params);
                    switch (HealthStatus)
                    {
                        case HealthStatus.InfectedIncubation:
                            HealthStatus = HealthStatus.InfectedSpread;
                            stateChangeMinutes = currentMinutes + (int)person.Context.Random.RollWeibull(param.IncubationToSpreadDelay.Shape * 24 * 60, param.IncubationToSpreadDelay.Scale * 24 * 60);
                            break;

                        case HealthStatus.InfectedSpread:
                            if (person.Context.Random.RollBinary(param.DeathProbability))
                            {
                                HealthStatus = HealthStatus.Dead;
                                (person.Behaviour as ConfigurableBehaviour).GetCurrentFacilities().ForEach(x=>x.RemovePersonInf(person));
                                person.SetLocation(null);
                            }
                            else
                            {
                                HealthStatus = HealthStatus.Recovered;
                                Infected = false;
                            }

                            break;
                    }
                }
            }

            // if (HealthStatus == HealthStatus.InfectedIncubation && person.Context.CurrentTime.Day != currentDay)
            // {
            //     HealthStatus = HealthStatus.InfectedSpread;
            //     currentDay = -1;
            // }
        }

        public bool TryInfect()
        {
            if (HealthStatus == HealthStatus.Susceptible)
            {
                HealthStatus = HealthStatus.InfectedIncubation;
                Infected = true;

                int currentMinutes = person.Context.CurrentTime.TotalMinutes;
                var param = ((ConfigParamsSimple)person.Context.Params);
                stateChangeMinutes = currentMinutes + (int)person.Context.Random.RollWeibull(param.IncubationToSpreadDelay.Shape * 24 * 60, param.IncubationToSpreadDelay.Scale * 24 * 60);

                // currentDay = person.Context?.CurrentTime.Day ?? -1;
                return true;
            }
            else
            {
                return false;
            }
           
        }

        public bool TryInfect(ConfigParamsSimple param, CityTime currentTime, Random random)
        {
            if (HealthStatus == HealthStatus.Susceptible)
            {
                HealthStatus = HealthStatus.InfectedIncubation;
                Infected = true;

                int currentMinutes = currentTime.TotalMinutes;
                stateChangeMinutes = currentMinutes + (int)random.RollWeibull(param.IncubationToSpreadDelay.Shape * 24 * 60, param.IncubationToSpreadDelay.Scale * 24 * 60);

                // currentDay = person.Context?.CurrentTime.Day ?? -1;
                return true;
            }
            else
            {
                return false;
            }

        }
    }
}
