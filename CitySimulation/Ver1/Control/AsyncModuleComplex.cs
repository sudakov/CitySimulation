using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CitySimulation.Control
{
    public class AsyncModuleComplex<T> : AsyncModule where T : Entities.EntityBase
    {
        private List<T> entities;

        public AsyncModuleComplex(List<T> entities, Controller controller)
        {
            this.entities = entities;

            entities.ForEach(x=>x.Context = controller.Context);
        }

        public override void RunAsync(Barrier barrier)
        {
            for (int i = 0; i < Controller.TestLoopsCount; i++)
            {
                PreProcess();

                barrier.SignalAndWait();

                Process();

                barrier.SignalAndWait();

                PostProcess();

                barrier.SignalAndWait();

                barrier.SignalAndWait();
            }

            while (Controller.IsRunning)
            {
                PreProcess();

                barrier.SignalAndWait();

                Process();

                barrier.SignalAndWait();

                PostProcess();

                barrier.SignalAndWait();

                barrier.SignalAndWait();
            }
        }

        public void Process()
        {
            for (var i = 0; i < entities.Count; i++)
            {
                entities[i].Process();
            }

        }

        public void PreProcess()
        {
            for (var i = 0; i < entities.Count; i++)
            {
                entities[i].PreProcess();
            }
        }

        public void PostProcess()
        {
            for (var i = 0; i < entities.Count; i++)
            {
                entities[i].PostProcess();
            }
        }
    }
}
