using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entity;
using CitySimulation.Tools;

namespace CitySimulation.Behaviour
{
    public class ComplexBehaviour : PunctualWorkerBehaviour
    {
        public bool[] WorkDays = new bool[7]{ true, true, true, true, true, false, false };
        public List<(string, TimeRange)>[] VisitShedule { get; set; }
        public ComplexBehaviour()
        {
        }

        public ComplexBehaviour(Facility workPlace, TimeRange workTime) : base(workPlace, workTime)
        {
        }

        public override EntityAction UpdateAction(Person person, CityTime dateTime, int deltaTime)
        {
            int day = dateTime.Day;
            int minutes = dateTime.Minutes;

            bool shouldWork = false;

            if (WorkDays[day])
            {
                if (!workTime.Reverse)
                {
                    shouldWork = workTime.End > minutes && (workTime.Start + _correction) <= minutes;
                }
                else
                {
                    shouldWork = workTime.End >= minutes || (workTime.Start + _correction) <= minutes;
                }
            }
           

            if (shouldWork && workPlace != null)
            {
                if (person.Location == workPlace)
                {
                    if (!(person.CurrentAction is Working))
                    {
                        SetAction(person, StandardActions.Working);
                    }
                }
                else
                {
                    Move(person, workPlace, deltaTime);
                }
            }
            else
            {
                if (person.Location == person.Home)
                {
                    if (!(person.CurrentAction is Resting))
                    {
                        SetAction(person, StandardActions.Resting);
                    }
                }
                else
                {
                    Move(person, person.Home, deltaTime);
                }
            }

            return person.CurrentAction;
        }

        // public Facility GetFacilityToVisit(CityTime dateTime)
        // {
        //     List<(string, TimeRange)> list = VisitShedule[dateTime.Day];
        //     for (int i = 0; i < list.Count; i++)
        //     {
        //         list[i].Item2
        //     }
        // }
    }
}
