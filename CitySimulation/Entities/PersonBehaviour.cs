using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CitySimulation.Behaviour.Action;
using CitySimulation.Navigation;
using CitySimulation.Tools;
using CitySimulation.Ver1.Entity;
using Range = CitySimulation.Tools.Range;

namespace CitySimulation.Entities
{
    public abstract class PersonBehaviour
    {
        public int Speed = 83 * 10000;

        protected List<Appointment> _appoints = new List<Appointment>(2);
        public Appointment CurrentAppointment;
        public int AppointmentInterval = 60;

        public abstract void UpdateAction(Person person, in CityTime dateTime, in int deltaTime);

        public void Move(Person person, Facility destination, in int deltaTime)
        {
            if (person.CurrentAction is Moving moving && moving.Destination == destination)
            {
                if (moving is CarMoving carMoving)
                {
                    moving.DistanceCovered += deltaTime * carMoving.Car.Speed;

                    if (moving.DistanceCovered >= carMoving.Link.Length)
                    {
                        person.SetLocation(moving.Link.To);
                        carMoving.Car.Location = moving.Link.To;
                        SetAction(person, null);
                    }
                }
                else
                {
                    if (person.Location == moving.Link.To)
                    {
                        SetAction(person, null);
                    }
                    else if (person.Location is Station from_station && moving.Link.To is Station)
                    // else if (person.Location is Station from_station && moving.Link.To is Station to_station)
                    {
                        foreach (var bus in from_station.Buses)
                        {
                            if (bus.HavePlace)
                            {
                                Station closest = bus.GetClosest(moving.Link.To);
                                if (closest != from_station && closest != null)
                                {
                                    person.CurrentAction = new Moving(person.Context.Routes[(from_station, closest)].Link, destination);
                                    person.SetLocation(bus);
                                    break;
                                }
                                else
                                {
                                    var a = 3;
                                    Station closest2 = bus.GetClosest(moving.Link.To);

                                }
                                // foreach (Link link in bus.StationsQueue)
                                // {
                                //     if (link.To == person.Location)
                                //     {
                                //         break;
                                //     }
                                //     else if (link.To == to_station)
                                //     {
                                //         person.SetLocation(bus);
                                //         break;
                                //     }
                                // }
                            }
                        }
                    }
                    else if (person.Location is Transport bus)
                    {
                        if (bus.Station == moving.Link.To)
                        {
                            person.SetLocation(moving.Link.To);
                            SetAction(person, null);
                        }
                    }
                    else
                    {
                        moving.DistanceCovered += deltaTime * Speed;

                        if (moving.DistanceCovered >= moving.Link.Length)
                        {
                            person.SetLocation(moving.Link.To);
                            SetAction(person, null);
                        }
                    }
                }
               
            }

            if (person.Location != destination && !(person.CurrentAction is Moving moving2 && moving2.Destination == destination))
            {
                if (person.Location is Transport bus)
                {
                    //Here person aborts his movement to begin one to new destination
                    if (bus.Station != null)
                    {
                        person.SetLocation(bus.Station);
                        SetAction(person, null);
                    }
                }
                else
                {
                    if (person.CurrentAction is CarMoving carMoving)
                    {
                        //Here person aborts his movement to begin one to new destination
                        double delta = carMoving.DistanceCovered / carMoving.Link.Length; 
                        Point current = carMoving.Link.From.Coords + new Point((int)(carMoving.Link.To.Coords.X * delta), (int)(carMoving.Link.To.Coords.Y * delta));
                        int distance = (int)Point.Distance(current, destination.Coords);

                        var action = new CarMoving(person.Car, carMoving.Link.From, destination);
                        action.DistanceCovered = (int)action.Link.Length - distance;

                        SetAction(person, action);
                        person.SetLocation(null);
                    }
                    else
                    {

                        bool useCar = false;
                        if (person.Car != null && person.Location == person.Car.Location)
                        {
                            if (person.Location == person.Home)
                            {
                                useCar = person.Context.Random.NextDouble() < 0.7f;
                            }
                            else
                            {
                                useCar = true;
                            }
                        }

                        if (useCar)
                        {
                            SetAction(person, new CarMoving(person.Car, person.Location, destination));
                            person.SetLocation(null);
                        }
                        else
                        {
                            Link link;
                            try
                            {
                                link = person.Context.Routes[(person.Location, destination)].Link;
                            }
                            catch (Exception)
                            {
                                throw new InvalidDataException($"Route between {person.Location} and {destination} required");
                            }

                            SetAction(person, new Moving(link, destination));
                        }
                    }
                }
            }
        }

        public double TimeToPlace(Person person, Facility destination)
        {
            Facility current;
            if (person.CurrentAction is CarMoving carMoving)
            {
                current = carMoving.Destination;
            }
            else if (person.Location is Transport bus)
            {
                current = bus.StationsQueue.Peek().To;
            }
            else
            {
                current = person.Location;
            }

            if (current == destination)
            {
                return 0;
            }

            return person.Context.Routes[(current, destination)].TotalLength / Speed;
        }

        public virtual void Setup(Person person)
        {
            
        }

        public virtual EntityAction SetAction(Person person, EntityAction action)
        {
            person.CurrentAction = action;
            return action;
        }



        public virtual bool AppointVisit(in Facility facility, in LogCityTime time, in int duration, in bool force = false)
        {
            return false;
        }

        public void SortAppointments(Person person)
        {
            //if (_appoints.Any(x=>x.Time.Day < Controller.CurrentTime.Day))
            //{
            //    int a = 0;
            //}

            _appoints.RemoveAll(x => x.Time.Day < person.Context.CurrentTime.Day);
            if (_appoints.Count != 0)
            {
                _appoints = _appoints.OrderByDescending(x => x.Time).ToList();
            }
        }
        //LinkedList<Facility> lst = new LinkedList<Facility>();
        protected void AssignAppointment(Person person, int day, int minutes)
        {
            //lst.AddLast(person.Location);
            //if (minutes > 60 * 22 && _appoints.Any(x=>x.Facility is Store))
            //{
            //    int a = 3;
            //}
            if (_appoints.Count != 0 && CurrentAppointment == null)
            {
                Appointment appointment = _appoints[^1];

                if (appointment.Time.Day == day)
                {
                    int timeToAppoint = appointment.Time.Minutes - minutes;
                    if ((int)person.Context.Routes.LongestRoute/Speed > timeToAppoint && (person.Location == appointment.Facility || TimeToPlace(person, appointment.Facility) > timeToAppoint))
                    {
                        CurrentAppointment = appointment;
                    }
                }
            }
        }

        protected void ProcessCurrentAppointment(Person person, in CityTime dateTime, in int deltaTime)
        {
            if (CurrentAppointment.Facility is Service service && service.WorkTime.End < dateTime.Minutes)
            {
                _appoints.Remove(CurrentAppointment);
                CurrentAppointment = null;
                return;
            }

            if (person.CurrentAction is ServiceVisiting serviceVisiting)
            {
                serviceVisiting.RemaningMinutes -= deltaTime;
                if (serviceVisiting.RemaningMinutes <= 0)
                {
                    _appoints.Remove(CurrentAppointment);
                    CurrentAppointment = null;
                }
            }
            else if (person.Location == CurrentAppointment.Facility)
            {
                if (CurrentAppointment.Facility is Service service1)
                {
                    SetAction(person, service1.BeginVisit(CurrentAppointment.Duration));
                }
                else
                {
                    _appoints.Remove(CurrentAppointment);
                    CurrentAppointment = null;
                }
            }
            else
            {
                Move(person, CurrentAppointment.Facility, deltaTime);
            }
        }



        public virtual int? GetFreeTime(int day, in Range range)
        {
            if (range.Start >= range.End)
            {
                return null;
            }

            Appointment[] appointments = _appoints.Where(x=>x.Time.Day == day).ToArray();
            int start = range.Random(Controller.Random);

            if (appointments.Length != 0)
            {
                int delta = range.Length / 10;

                int dayMinutes = day * 60 * 24;

                for (int i = start; i < range.End; i += delta)
                {
                    if (appointments.All(x => !x.TimeRange.InRange(dayMinutes + i)))
                    {
                        return i;
                    }
                }
                for (int i = 0; i < start; i += delta)
                {
                    if (appointments.All(x => !x.TimeRange.InRange(dayMinutes + i)))
                    {
                        return i;
                    }
                }

                return null;
            }

            return start;
        }
    }
}
