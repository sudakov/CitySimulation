using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entity;
using CitySimulation.Tools;

namespace CitySimulation.Behaviour
{
    public class PunctualWorkerBehaviour : WorkerBehaviour
    {
        private const int PositiveDeltaCountToChange = 5;

        protected int _correction;
        public int Tolerance = 5;
        public int MaxCorrection = 5 * 60;

        private int _positiveDeltaCounter = 0;

        public PunctualWorkerBehaviour(Facility home) : base(home)
        {
        }
        public PunctualWorkerBehaviour(Facility home, Facility workPlace, TimeRange workTime) : base(home, workPlace, workTime)
        {
        }

        public override void Setup(Person person)
        {
            base.Setup(person);
            _correction = Controller.Random.Next(-MaxCorrection / 5, MaxCorrection / 5);
        }

        public override EntityAction UpdateAction(Person person, CityTime dateTime, int deltaTime)
        {
            int minutes = dateTime.Minutes;

            bool shouldWork;
            if (!workTime.Reverse)
            {
                shouldWork = workTime.End > minutes && (workTime.Start + _correction) <= minutes;
            }
            else
            {
                shouldWork = workTime.End >= minutes || (workTime.Start + _correction) <= minutes;
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
                if (person.Location == home)
                {
                    if (!(person.CurrentAction is Resting))
                    {
                        SetAction(person, StandardActions.Resting);
                    }
                }
                else
                {
                    Move(person, home, deltaTime);
                }
            }

            return person.CurrentAction;
        }

        public override EntityAction SetAction(Person person, EntityAction action)
        {
            if (action is Working && !(person.CurrentAction is Working))
            {
                //Если дело происходит в полуночь, будут проблемы
                int delta = workTime.Start - Controller.CurrentTime.Minutes;
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

                    // if (delta < 0)
                    // {
                    //     float rand = Controller.Random.Next(0, 10) / 10f;
                    //     _correction = (int)(Math.Clamp(_correction + delta, -MaxCorrection, MaxCorrection) * rand + _correction * (1 - rand));
                    // }

                    // _correction = delta > 0 ? Controller.Random.Next(0, delta) : -Controller.Random.Next(0, -delta);
                    // Debug.WriteLine("correction " + person.Name + ": " + CityTime.MinutesToTime(_correction));
                }
            }
            return base.SetAction(person, action);
        }
    }
}
