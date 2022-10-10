using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CitySimulation.Entities;
using CitySimulation.Tools;

namespace CitySimulation.Control
{
    public class ControllerComplex : Controller
    {
        public override int? RunAsync(int threads)
        {
            IsRunning = true;
            Paused = false;

            var sessionId = Logger?.Start();


            var facilityControllers = new AsyncModuleComplex<Facility>[threads];
            var personControllers = new AsyncModuleComplex<Person>[threads];

            var delta = City.Persons.Count / threads;

            for (var i = 0; i < threads - 1; i++)
            {
                personControllers[i] = new AsyncModuleComplex<Person>(City.Persons
                    .Skip(i * delta)
                    .Take(delta).ToList(), this);
                facilityControllers[i] = new AsyncModuleComplex<Facility>(City.Facilities.Values
                    .Skip(i * delta)
                    .Take(delta).ToList(), this);
            }

            personControllers[threads - 1] = new AsyncModuleComplex<Person>(City.Persons.Skip((threads - 1) * delta).ToList(), this);
            facilityControllers[threads - 1] = new AsyncModuleComplex<Facility>(City.Facilities.Values.Skip((threads - 1) * delta).ToList(), this);

            var barrier = new Barrier(personControllers.Length + facilityControllers.Length + 1);

            PreRun();

            foreach (var module in personControllers) Task.Run(() => module.RunAsync(barrier));

            foreach (var module in facilityControllers) Task.Run(() => module.RunAsync(barrier));


            TimeLogger.Log($">> {TestLoopsCount} stop start");

            for (var i = 0; (i < TestLoopsCount) & IsRunning; i++)
            {
                while (Paused)
                {
                }

                // Logger?.PreProcess();
                for (var i1 = 0; i1 < Modules.Count; i1++) Modules[i1].PreProcess();

                barrier.SignalAndWait();

                // Logger?.Process();
                for (var i1 = 0; i1 < Modules.Count; i1++) Modules[i1].Process();

                barrier.SignalAndWait();

                // Logger?.PostProcess();
                for (var i1 = 0; i1 < Modules.Count; i1++) Modules[i1].PostProcess();

                barrier.SignalAndWait();

                Context.CurrentTime.AddSeconds(DeltaTime);
                CallOnLifecycleFinished();
                if (SleepTime != 0) Thread.Sleep(SleepTime);

                barrier.SignalAndWait();
            }

            TimeLogger.Log($"<< {TestLoopsCount} stop finish");
            while (IsRunning)
            {
                while (Paused)
                {
                }

                // Logger?.PreProcess();
                for (var i1 = 0; i1 < Modules.Count; i1++) Modules[i1].PreProcess();

                barrier.SignalAndWait();

                // Logger?.Process();
                for (var i1 = 0; i1 < Modules.Count; i1++) Modules[i1].Process();

                barrier.SignalAndWait();

                // Logger?.PostProcess();
                for (var i1 = 0; i1 < Modules.Count; i1++) Modules[i1].PostProcess();

                barrier.SignalAndWait();

                Context.CurrentTime.AddSeconds(DeltaTime);
                CallOnLifecycleFinished();
                if (SleepTime != 0) Thread.Sleep(SleepTime);

                barrier.SignalAndWait();
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

            int? sessionId = Logger?.Start();

            PreRun();

            TimeLogger.Log($">> {TestLoopsCount} stop start");

            for (int i = 0; i < TestLoopsCount && IsRunning; i++)
            {
                while (Paused) { }

                DoCycle(Context.CurrentTime);
                Context.CurrentTime.AddSeconds(DeltaTime);
                CallOnLifecycleFinished();
                if (SleepTime != 0)
                {
                    Thread.Sleep(SleepTime);
                }
            }

            TimeLogger.Log($"<< {TestLoopsCount} stop finish");

            while (IsRunning)
            {
                while (Paused) { }

                DoCycle(Context.CurrentTime);
                Context.CurrentTime.AddSeconds(DeltaTime);
                CallOnLifecycleFinished();
                if (SleepTime != 0)
                {
                    Thread.Sleep(SleepTime);
                }
            }

            Modules.ForEach(x => x.Finish());


            TimeLogger.Log(">> Logger stop start");
            Logger?.Stop();
            TimeLogger.Log("<< Logger stop finish");

            return sessionId;
        }

        public override int? Run(int loopsCount)
        {
            SetContextForAll(Context);

            int? sessionId = Logger?.Start();

            PreRun();

            for (int i = 0; i < loopsCount; i++)
            {
                while (Paused) { }

                DoCycle(Context.CurrentTime);
                Context.CurrentTime.AddSeconds(DeltaTime);
                CallOnLifecycleFinished();
                if (SleepTime != 0)
                {
                    Thread.Sleep(SleepTime);
                }
            }

            Modules.ForEach(x => x.Finish());

            Logger?.Stop();

            return sessionId;
        }

        private void DoCycle(CityTime currentTime)
        {
            // foreach (Facility facility in City.Facilities.Values)
            // {
            //     facility.PreProcess();
            // }

            // foreach (Person person in City.Persons)
            // {
            //     person.PreProcess();
            // }

            // foreach (Facility facility in City.Facilities.Values)
            // {
            //     facility.Process();
            // }

            // foreach (Person person in City.Persons)
            // {
            //     person.Process();
            // }

            // foreach (Facility facility in City.Facilities.Values)
            // {
            //     facility.PostProcess();
            // }

            // foreach (Person person in City.Persons)
            // {
            //     person.PostProcess();
            // }


            for (int i = 0; i < City.Facilities.Count; i++)
            {
                City.Facilities[i].PreProcess();
            }


            for (var i = 0; i < City.Persons.Count; i++)
            {
                City.Persons[i].PreProcess();
            }

            // Logger.PreProcess();
            for (var i = 0; i < Modules.Count; i++)
            {
                Modules[i].PreProcess();
            }

            for (int i = 0; i < City.Facilities.Count; i++)
            {
                City.Facilities[i].Process();
            }


            for (var i = 0; i < City.Persons.Count; i++)
            {
                City.Persons[i].Process();
            }

            // Logger.Process();
            for (var i = 0; i < Modules.Count; i++)
            {
                Modules[i].Process();
            }

            for (int i = 0; i < City.Facilities.Count; i++)
            {
                City.Facilities[i].PostProcess();
            }


            for (var i = 0; i < City.Persons.Count; i++)
            {
                City.Persons[i].PostProcess();
            }

            // Logger.PostProcess();
            for (var i = 0; i < Modules.Count; i++)
            {
                Modules[i].PostProcess();
            }
        }

    }
}