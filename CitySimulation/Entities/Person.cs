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

            if (HealthData.HealthStatus != HealthStatus.Dead)
            {
                Behaviour?.UpdateAction(this, Context.CurrentTime, Controller.Instance.DeltaTime);
                HealthData.Process();
            }
        }

        public void SetLocation(Facility facility)
        {
            if (Location != null)
            {
                LogCityTime prev_time = _facilityEnterTime;
                _facilityEnterTime = new LogCityTime(Context.CurrentTime);

                Context.Logger?.LogPersonInFacilityTime(prev_time, _facilityEnterTime, Location, this);

            }

            if (Location != facility)
            {
                Location?.RemovePerson(this);
                facility?.AddPerson(this);
                Location = facility;
            }
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

        public override Point? CalcCoords()
        {
            var loc = Location;
            if (loc is Transport)
            {
                return loc.CalcCoords();
            }
            else if(loc != null)
            {
                if (CurrentAction is Moving moving)
                {
                    double k = moving.DistanceCovered / moving.Link.Length;
                    return (1 - k) * moving.Link.From.CalcCoords() + k * moving.Link.To.CalcCoords();
                }
                else
                {
                    return loc.Coords;
                }
            }

            return null;
        }
    }

    public enum Gender
    {
        Male, Female
    }
}
