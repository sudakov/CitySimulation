using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entities;

namespace CitySimulation.Ver2.Entity.Behaviour
{
    public class ConfigurableBehaviourWithTransport : ConfigurableBehaviour
    {
        protected Facility lastLocation;
        protected override void StartMoving(Person person, Facility facility, in int deltaTime)
        {
            if (facility != null)
            {
                if (person.Location != null)
                {
                    Move(person, facility, deltaTime);
                }
                else if (lastLocation != null)
                {
                    person.SetLocation(lastLocation);
                    Move(person, facility, deltaTime);
                }
                else
                {
                    person.SetLocation(facility);
                }
            }
            else
            {
                lastLocation = person.Location;
                person.SetLocation(null);
            }
        }
    }
}
