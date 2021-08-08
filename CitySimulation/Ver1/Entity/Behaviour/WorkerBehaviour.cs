using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entities;
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

        public Facility WorkPlace => attendPlace;
        public Range WorkTime => attendTime;
        public void SetWorkplace(Facility workplace, Range workTime)
        {
            attendPlace = workplace;
            attendTime = workTime;
        }
    }
}
