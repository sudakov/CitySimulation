using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Entities
{
    public abstract class FacilityBehaviour
    {
        public Facility Facility;
        public virtual void OnPersonAdd(Person p){}
        public virtual void OnPersonRemove(Person p){}
        public virtual void ProcessInfection(){}
    }
}
