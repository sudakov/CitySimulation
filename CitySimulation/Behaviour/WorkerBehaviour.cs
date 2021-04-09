using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entity;
using CitySimulation.Navigation;
using CitySimulation.Tools;

namespace CitySimulation.Behaviour
{
    public class WorkerBehaviour : PersonBehaviour, IPersonWithWork
    {
        protected Facility workPlace;

        protected TimeRange workTime = new TimeRange(8*60, 17*60);

        Facility IPersonWithWork.WorkPlace { get => workPlace; }

        void IPersonWithWork.SetWorkplace(Facility workplace)
        {
            this.workPlace = workplace;
            if (workplace is IWorkplace iWorkplace)
            {
                this.workTime = iWorkplace.WorkTime;
            }
        }

        public WorkerBehaviour()
        {
        }

        public WorkerBehaviour(Facility workPlace, TimeRange workTime)
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

        public override EntityAction UpdateAction(Person person, CityTime dateTime, int deltaTime)
        {
            int minutes = dateTime.Minutes;

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

            return person.CurrentAction;
        }
    }
}
