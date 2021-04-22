using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using CitySimulation.Behaviour;
using CitySimulation.Behaviour.Action;
using CitySimulation.Tools;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Entity
{
    public class Person : Entity
    {
        public int Age;
        public Gender Gender;
        public Family Family;
        public LivingHouse Home;

        private Facility _location = null;
        private List<Facility> history = new List<Facility>();
        public Facility Location
        {
            get { return _location; }
            set { _location = value; history.Add(value); }
        }

        public HealthData HealthData;


        // public Facility Location
        // {
        //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //     get; 
        //     private set;
        // }
        public PersonBehaviour Behaviour;
        public EntityAction CurrentAction;

        private LogCityTime _facilityEnterTime;

        public Person(string name) : base(name)
        {
        }

        public override void Process()
        {
            base.Process();
            Behaviour?.UpdateAction(this, Controller.CurrentTime, Controller.Instance.DeltaTime);
            HealthData.Process();
        }

        public void SetLocation(Facility facility)
        {
            if (Location != null)
            {
                LogCityTime prev_time = _facilityEnterTime;
                _facilityEnterTime = new LogCityTime(Controller.CurrentTime);

                Controller.Instance.Logger?.LogPersonInFacilityTime(prev_time, _facilityEnterTime, Location, this);

            }

            Location?.RemovePerson(this);
            Location = facility;
            facility?.AddPerson(this);
        }

        public override void Setup(Controller controller)
        {
            base.Setup(controller);
            if (Location == null)
            {
                SetLocation(Home);
            }

            Behaviour?.Setup(this);
        }

    }

    public enum Gender
    {
        Male, Female
    }
}
