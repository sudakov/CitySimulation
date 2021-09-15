using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CitySimulation.Behaviour;
using CitySimulation.Entities;
using CitySimulation.Health;
using CitySimulation.Tools;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Control
{
    public class VirusSpreadModule : Module
    {
        public override void Setup(Controller controller)
        {
            base.Setup(controller);
            foreach (Person person in Controller.Instance.City.Persons.Shuffle(Controller.Random).Where(x=>x.Behaviour is IPersonWithWork).Take(1))
            {
                if (person.HealthData.TryInfect())
                {
                    person.Location?.Infectors.Add(person);
                }
            }
        }


        public override void PostProcess()
        {
            foreach (Person person in Controller.Instance.City.Persons)
            {
                if (person.HealthData.HealthStatus == HealthStatus.Susceptible)
                {
                    if (person.Location?.Infectors.Count > 0)
                    {
                        if (person.HealthData.TryInfect())
                        {
                            person.Location?.Infectors.Remove(person.Location.Infectors.First());
                        }
                    }
                }
            }

            // Debug.WriteLine("Инфицированно: " + Controller.Instance.City.Persons.Count(x=>x.HealthData.Infected));
            //
            // foreach (Facility facility in Controller.Instance.City.Facilities.Values)
            // {
            //     if (facility.InfectionPoints > 0)
            //     {
            //         facility.InfectionPoints--;
            //     }
            // }
        }

    }
}
