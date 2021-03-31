using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace CitySimulation
{
    public class AsyncModule<T> where T : Entity.Entity
    {
        public List<T> entities;

        public AsyncModule(List<T> entities)
        {
            this.entities = entities;
        }

        public void RunAsync(Barrier barrier)
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
