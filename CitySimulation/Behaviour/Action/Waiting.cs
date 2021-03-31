using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Behaviour.Action
{
    public class Waiting : EntityAction
    {
        public int RemainingTime;

        public Waiting(int remainingTime)
        {
            RemainingTime = remainingTime;
        }

        public override string ToString()
        {
            return "Waiting: " + RemainingTime;
        }
    }
}
