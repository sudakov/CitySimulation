using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entity;
using CitySimulation.Tools;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Behaviour
{
    public class ComplexBehaviour : PunctualWorkerBehaviour
    {
        public bool[] WorkDays = new bool[7]{ true, true, true, true, true, false, false };
        public List<(string, Range)>[] VisitShedule { get; set; }
        public ComplexBehaviour()
        {
        }

        public ComplexBehaviour(Facility workPlace, Range workTime) : base(workPlace, workTime)
        {
        }

        public override EntityAction UpdateAction(Person person, in CityTime dateTime, in int deltaTime)
        {
            int day = dateTime.Day;
            int minutes = dateTime.Minutes;

            bool shouldWork = false;

            if (WorkDays[day])
            {
                if (!attendTime.Reverse)
                {
                    shouldWork = attendTime.End > minutes && (attendTime.Start + _correction) <= minutes;
                }
                else
                {
                    shouldWork = attendTime.End >= minutes || (attendTime.Start + _correction) <= minutes;
                }
            }
           

            if (shouldWork && attendPlace != null)
            {
                if (person.Location == attendPlace)
                {
                    if (!(person.CurrentAction is Working))
                    {
                        SetAction(person, StandardActions.Working);
                    }
                }
                else
                {
                    Move(person, attendPlace, deltaTime);
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
