using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Behaviour.Action
{
    public class ServiceVisiting : EntityAction
    {
        public int RemaningMinutes;

        public ServiceVisiting(int remaningMinutes)
        {
            RemaningMinutes = remaningMinutes;
        }
    }
}
