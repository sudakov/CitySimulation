using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entity;
using CitySimulation.Tools;

namespace CitySimulation.Behaviour
{
    public class RegularAttendBehaviour : PersonBehaviour
    {
        protected Facility attendPlace;
        protected TimeRange attendTime = new TimeRange(8 * 60, 17 * 60);

        private const int PositiveDeltaCountToChange = 5;

        protected int _correction;
        public int Tolerance = 5;
        public int MaxCorrection = 5 * 60;

        private int _positiveDeltaCounter = 0;

        public RegularAttendBehaviour()
        {
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

        public override EntityAction UpdateAction(Person person, CityTime dateTime, int deltaTime)
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

            return person.CurrentAction;
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
                            _correction = Math.Clamp(_correction + delta / 4, -MaxCorrection, 0);
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
