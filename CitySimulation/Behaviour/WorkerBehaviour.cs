using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entity;
using CitySimulation.Tools;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Behaviour
{
    public class WorkerBehaviour : RegularAttendBehaviour, IPersonWithWork
    {
        public WorkerBehaviour()
        {
        }

        public WorkerBehaviour(Facility workPlace, Range workTime)
        {
            SetWorkplace(workPlace);
            attendTime = workTime;
        }

        public Facility WorkPlace => attendPlace;
        public void SetWorkplace(Facility workplace)
        {
            attendPlace = workplace;
        }
    }
}
