using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entity;
using CitySimulation.Tools;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Behaviour
{
    public class PunctualWorkerBehaviour : RegularAttendBehaviour, IPersonWithWork
    {
        private const int PositiveDeltaCountToChange = 5;

        protected int _correction;
        public int Tolerance = 5;
        public int MaxCorrection = 5 * 60;

        private int _positiveDeltaCounter = 0;

        public PunctualWorkerBehaviour()
        {
        }
        public PunctualWorkerBehaviour(Facility workPlace, Range workTime)
        {
            SetWorkplace(workPlace);
            attendTime = workTime;
        }

        public Facility WorkPlace => attendPlace;
        public void SetWorkplace(Facility workplace)
        {
            attendPlace = workplace;
        }

        public override void Setup(Person person)
        {
            base.Setup(person);
            if (attendPlace != null)
            {
                int maxHomeToWorkTime = (int)Controller.Instance.Routes[(person.Home, attendPlace)].TotalLength / Speed;

                _correction = Controller.Random.Next(-maxHomeToWorkTime, Tolerance);
            }
        }

        public override void UpdateAction(Person person, in CityTime dateTime, in int deltaTime)
        {
            int minutes = dateTime.Minutes;

            bool shouldWork;
            if (!attendTime.Reverse)
            {
                shouldWork = attendTime.End > minutes && (attendTime.Start + _correction) <= minutes;
            }
            else
            {
                shouldWork = attendTime.End >= minutes || (attendTime.Start + _correction) <= minutes;
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

        }

        public override EntityAction SetAction(Person person, EntityAction action)
        {
            if (action is Working && !(person.CurrentAction is Working))
            {
                //Если дело происходит в полуночь, будут проблемы
                int delta = attendTime.Start - Controller.CurrentTime.Minutes;
                if (Math.Abs(delta) > Tolerance)
                {
                    if (delta > 0)
                    {
                        if (_positiveDeltaCounter > PositiveDeltaCountToChange)
                        {
                            _correction = Math.Clamp(_correction + delta/4, -MaxCorrection, 0);
                        }
                    }
                    else
                    {
                        _positiveDeltaCounter = 0;
                        _correction = Math.Clamp(_correction + (delta + delta / 2), -MaxCorrection, 0);
                    }
                }
            }
            return base.SetAction(person, action);
        }
    }
}
