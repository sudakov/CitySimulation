using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entities;
using CitySimulation.Navigation;
using CitySimulation.Tools;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Behaviour
{
    [Obsolete]
    public class WorkerLiteBehaviour : PersonBehaviour, IPersonWithWork
    {
        protected Facility workPlace;

        protected Range workTime = new Range(8*60, 17*60);

        Facility IPersonWithWork.WorkPlace { get => workPlace; }

        void IPersonWithWork.SetWorkplace(Facility workplace, Range workRange)
        {
            this.workPlace = workplace;
            if (workplace is IWorkplace iWorkplace)
            {
                this.workTime = iWorkplace.WorkTime;
            }
        }

        public WorkerLiteBehaviour()
        {
        }

        public WorkerLiteBehaviour(Facility workPlace, Range workTime)
        {
            this.workPlace = workPlace;
            this.workTime = workTime;
        }

        protected virtual bool ShouldWork(int minutes)
        {
            if (!workTime.Reverse)
            {
                return workTime.End > minutes && workTime.Start <= minutes;
            }
            else
            {
                return workTime.End >= minutes || workTime.Start <= minutes;
            }
        }

        public override void UpdateAction(Person person, in CityTime dateTime, in int deltaTime)
        {
            int minutes = dateTime.Seconds / 60;

            bool shouldWork;
            if (!workTime.Reverse)
            {
                shouldWork = workTime.End > minutes && workTime.Start <= minutes;
            }
            else
            {
                shouldWork = workTime.End >= minutes || workTime.Start <= minutes;
            }

            if (shouldWork)
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
        }
    }
}
