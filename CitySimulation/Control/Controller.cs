using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CitySimulation.Control;
using CitySimulation.Control.Log;
using CitySimulation.Control.Log.DbModel;
using CitySimulation.Entity;
using CitySimulation.Navigation;


namespace CitySimulation
{
    public class Controller
    {

        public const int TestLoopsCount = 20000;
        public const int TestPersonsCount = 1000;
        public static Controller Instance /*{ get; private set; }*/;

        public event Action OnLifecycleFinished = delegate {};

        public City City;
        public RouteTable Routes;

        public static CityTime CurrentTime/* { get; private set; }*/ = new CityTime();
        public static Logger Logger = new DBLogger();// { get; private set; }
        public int DeltaTime/* { get; set; }*/ = 5;
        public int SleepTime { get; set; } = 0;

        public static bool IsRunning { get; set; }
        public static bool Paused { get; set; }

        public static Random Random { get; } = new Random();

        public Controller()
        {
            if (Instance != null)
            {
                throw new Exception("Singleton already exists");
            }

            Instance = this;
        }

        public void Setup()
        {
            Routes = City.Facilities.CreateRouteTable();
            CurrentTime = new CityTime();
            foreach (Bus bus in City.Facilities.Values.Where(x=>x is Bus))
            {
                bus.SetupRoute(Routes);
            }

            foreach (Person person in City.Persons)
            {
                person.Setup();
            }
        }

        public int? RunAsync()
        {
            IsRunning = true;
            Paused = false;

            int? sessionId = Logger?.Start();

            int split = 3;

            AsyncModule<Facility>[] facilityControllers = new AsyncModule<Facility>[split];
            AsyncModule<Person>[] personControllers = new AsyncModule<Person>[split];

            int delta = City.Persons.Count / split;

            for (int i = 0; i < split - 1; i++)
            {
                personControllers[i] = new AsyncModule<Person>(City.Persons
                    .Skip(i * delta)
                    .Take(delta).ToList());
                facilityControllers[i] = new AsyncModule<Facility>(City.Facilities.Values
                    .Skip(i * delta)
                    .Take(delta).ToList());
            }
            personControllers[split - 1] = new AsyncModule<Person>(City.Persons.Skip((split - 1) * delta).ToList());
            facilityControllers[split - 1] = new AsyncModule<Facility>(City.Facilities.Values.Skip((split - 1) * delta).ToList());

            Barrier barrier = new Barrier(personControllers.Length + facilityControllers.Length + 1);



            Debug.WriteLine(CurrentTime.ToString());
            Console.WriteLine(CurrentTime.ToString());


            foreach (var module in personControllers)
            {
                Task.Run(() => module.RunAsync(barrier));
            }

            foreach (var module in facilityControllers)
            {
                Task.Run(() => module.RunAsync(barrier));
            }

            for (int i = 0; i < TestLoopsCount; i++)
            {
                while (Paused) { }

                barrier.SignalAndWait();
                barrier.SignalAndWait(); 
                barrier.SignalAndWait();
                CurrentTime.AddMinutes(DeltaTime);
                OnLifecycleFinished();
                if (SleepTime != 0)
                {
                    Thread.Sleep(SleepTime);
                }
                barrier.SignalAndWait();
            }
            Debug.WriteLine(CurrentTime.ToString());
            Console.WriteLine(CurrentTime.ToString());

            while (IsRunning)
            {
                while (Paused) { }

                barrier.SignalAndWait();
                barrier.SignalAndWait();
                barrier.SignalAndWait();
                CurrentTime.AddMinutes(DeltaTime);
                OnLifecycleFinished();
                if (SleepTime != 0)
                {
                    Thread.Sleep(SleepTime);
                }
                barrier.SignalAndWait();
            }

            Logger?.Stop();

            return sessionId;
        }

        public int? Run()
        {
            IsRunning = true;
            Paused = false;

            int? sessionId = Logger?.Start();

            Debug.WriteLine(CurrentTime.ToString());
            for (int i = 0; i < TestLoopsCount && IsRunning; i++)
            {
                while (Paused) { }

                DoCycle(CurrentTime);
                CurrentTime.AddMinutes(DeltaTime);
                OnLifecycleFinished();
                if (SleepTime != 0)
                {
                    Thread.Sleep(SleepTime);
                }
            }
            Debug.WriteLine(CurrentTime.ToString());


            while (IsRunning)
            {
                while (Paused) { }
            
                DoCycle(CurrentTime);
                CurrentTime.AddMinutes(DeltaTime);
                OnLifecycleFinished();
                if (SleepTime != 0)
                {
                    Thread.Sleep(SleepTime);
                }
            }

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
           

            for (int i = 0; i < City.Facilities.Count; i++)
            {
                City.Facilities[i].Process();
            }
            

            for (var i = 0; i < City.Persons.Count; i++)
            {
                City.Persons[i].Process();
            }

            for (int i = 0; i < City.Facilities.Count; i++)
            {
                City.Facilities[i].PostProcess();
            }

          

            for (var i = 0; i < City.Persons.Count; i++)
            {
                City.Persons[i].PostProcess();
            }
        }

    }
}
