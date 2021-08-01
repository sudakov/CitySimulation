using CitySimulation.Control.Log.DbModel;
using CitySimulation.Entity;
using CitySimulation.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CitySimulation.Control
{
    public class ControllerSimple : Controller
    {
        public override int? RunAsync(int numThreads)
        {
            IsRunning = true;
            Paused = false;

            var sessionId = Logger?.Start();

            var split = 3;

            var asyncModules = new AsyncModuleSimple[split];

            var delta = City.Persons.Count / split;

            for (var i = 0; i < split - 1; i++)
            {
                asyncModules[i] = new AsyncModuleSimple(
                    City.Persons.Skip(i * delta).Take(delta).ToList(), 
                    City.Facilities.Values.Skip(i * delta).Take(delta).ToList(),
                    this
                );
            }

            asyncModules[split - 1] = new AsyncModuleSimple(
                City.Persons.Skip((split - 1) * delta).ToList(), 
                City.Facilities.Values.Skip((split - 1) * delta).ToList(),
                this
            );

            PreRun();

            var barrier = new Barrier(asyncModules.Length + 1);

            foreach (var module in asyncModules) Task.Run(() => module.RunAsync(barrier));

            TimeLogger.Log($">> {TestLoopsCount} stop start");

            for (var i = 0; (i < TestLoopsCount) & IsRunning; i++)
            {
                while (Paused)
                {
                }

                Modules.ForEach(x => x.PreProcess());

                barrier.SignalAndWait();

                Modules.ForEach(x => x.Process());

                barrier.SignalAndWait();

                Modules.ForEach(x => x.PostProcess());

                barrier.SignalAndWait();

                Context.CurrentTime.AddMinutes(DeltaTime);
                CallOnLifecycleFinished();
                if (SleepTime != 0) Thread.Sleep(SleepTime);
            }

            TimeLogger.Log($"<< {TestLoopsCount} stop finish");
            while (IsRunning)
            {
                while (Paused)
                {
                }

                Modules.ForEach(x=>x.PreProcess());

                barrier.SignalAndWait();

                Modules.ForEach(x => x.Process());

                barrier.SignalAndWait();

                Modules.ForEach(x => x.PostProcess());

                barrier.SignalAndWait();

                Context.CurrentTime.AddMinutes(DeltaTime);
                CallOnLifecycleFinished();
                if (SleepTime != 0) Thread.Sleep(SleepTime);
            }

            Modules.ForEach(x => x.Finish());

            TimeLogger.Log(">> Logger stop start");
            Logger?.Stop();
            TimeLogger.Log("<< Logger stop finish");


            return sessionId;
        }

        public override int? Run(int loopsCount)
        {
            IsRunning = true;
            Paused = false;

            SetContextForAll(Context);

            var sessionId = Logger?.Start();

            PreRun();

            TimeLogger.Log($">> {TestLoopsCount} stop start");

            for (var i = 0; (i < TestLoopsCount) & IsRunning; i++)
            {
                while (Paused)
                {
                }

                Modules.ForEach(x => x.PreProcess());


                for (int j = 0; j < City.Persons.Count; j++)
                {
                    City.Persons[j].Process();
                }
                Modules.ForEach(x => x.Process());

                for (var j = 0; j < City.Facilities.Count; j++)
                {
                    City.Facilities[j].PostProcess();
                }
                Modules.ForEach(x => x.PostProcess());

                Context.CurrentTime.AddMinutes(DeltaTime);
                CallOnLifecycleFinished();
                if (SleepTime != 0) Thread.Sleep(SleepTime);
            }

            Modules.ForEach(x => x.Finish());

            TimeLogger.Log(">> Logger stop start");
            Logger?.Stop();
            TimeLogger.Log("<< Logger stop finish");

            return sessionId;
        }

        public override int? Run()
        {
            IsRunning = true;
            Paused = false;

            SetContextForAll(Context);

            int? sessionId = Logger?.SessionId;

            PreRun();

            TimeLogger.Log($">> {TestLoopsCount} stop start");

            for (var i = 0; (i < TestLoopsCount) & IsRunning; i++)
            {
                while (Paused)
                {
                }

                for (int j = 0; j < City.Persons.Count; j++)
                {
                    City.Persons[j].Process();
                }

                for (var j = 0; j < City.Facilities.Count; j++)
                {
                    City.Facilities[j].PostProcess();
                }

                Context.CurrentTime.AddMinutes(DeltaTime);
                CallOnLifecycleFinished();
                if (SleepTime != 0) Thread.Sleep(SleepTime);
            }

            TimeLogger.Log($"<< {TestLoopsCount} stop finish");
            while (IsRunning)
            {
                while (Paused)
                {
                }

                for (int j = 0; j < City.Persons.Count; j++)
                {
                    City.Persons[j].Process();
                }

                for (var j = 0; j < City.Facilities.Count; j++)
                {
                    City.Facilities[j].PostProcess();
                }

                Context.CurrentTime.AddMinutes(DeltaTime);
                CallOnLifecycleFinished();
                if (SleepTime != 0) Thread.Sleep(SleepTime);
            }

            Modules.ForEach(x=>x.Finish());

            TimeLogger.Log(">> Logger stop start");
            Logger?.Stop();
            TimeLogger.Log("<< Logger stop finish");

            return sessionId;
        }
    }
}
