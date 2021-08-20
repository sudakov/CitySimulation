using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Control
{
    public class Module
    {
        public Controller Controller;
        public virtual void Setup(Controller controller)
        {
            Controller = controller;
        }
        public virtual void PreProcess()
        {

        }

        public virtual void Process()
        {

        }

        public virtual void PostProcess()
        {

        }

        public virtual void PreRun()
        {

        }

        public virtual void Finish()
        {

        }

    }
}
