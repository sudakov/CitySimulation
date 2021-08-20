using System.Collections.Generic;
using CitySimulation.Behaviour.Action;
using CitySimulation.Health;
using CitySimulation.Tools;
using CitySimulation.Ver1.Entity;

namespace CitySimulation.Entities
{
    public class Person : EntityBase
    {
        public int Age;
        public Gender Gender;
        public Family Family;
        public LivingHouse Home;
        public Car Car;

        private Facility _location = null;
        // private List<Facility> history = new List<Facility>();
        public Facility Location
        {
            get { return _location; }
            private set { _location = value; /*history.Add(value);*/ }
        }

        public IHealthData HealthData;


        // public Facility Location
        // {
        //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //     get; 
        //     private set;
        // }
        public PersonBehaviour Behaviour;
        public EntityAction CurrentAction;

        private LogCityTime _facilityEnterTime;

        public Person()
        {

        }

        public Person(string name) : base(name)
        {
        }

        public override void Process()
        {
            base.Process();
            Behaviour?.UpdateAction(this, Context.CurrentTime, Controller.Instance.DeltaTime);
            HealthData.Process();
        }

        public void SetLocation(Facility facility)
        {
            if (Location != null)
            {
                LogCityTime prev_time = _facilityEnterTime;
                _facilityEnterTime = new LogCityTime(Context.CurrentTime);

                Context.Logger?.LogPersonInFacilityTime(prev_time, _facilityEnterTime, Location, this);

            }

            Location?.RemovePerson(this);
            Location = facility;
            facility?.AddPerson(this);
        }

        public override void Setup()
        {
            base.Setup();
            if (Location == null)
            {
                SetLocation(Home);
            }

            if (Car != null)
            {
                Car.Location = Home;
            }

            Behaviour?.Setup(this);
        }

    }

    public enum Gender
    {
        Male, Female
    }
}
