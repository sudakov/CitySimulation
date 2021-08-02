using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Behaviour.Action
{
    public static class StandardActions
    {
        public static readonly Working Working = new Working();
        public static readonly Resting Resting = new Resting();
    }
}
