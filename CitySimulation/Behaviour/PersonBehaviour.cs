using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Entity;
using CitySimulation.Navigation;

namespace CitySimulation.Behaviour
{
    public abstract class PersonBehaviour
    {
        public int Speed = 83;
        public abstract EntityAction UpdateAction(Person person, CityTime dateTime, int deltaTime);

        public void Move(Person person, Facility destination, int deltaTime)
        {
            if (person.CurrentAction is Moving moving && moving.Destination == destination)
            {
                if (person.Location == moving.Link.To)
                {
                    SetAction(person, null);
                }
                else if (person.Location is Station from_station && moving.Link.To is Station to_station)
                {
                    foreach (var bus in from_station.Buses)
                    {
                        if (bus.HavePlace)
                        {
                            foreach (Link link in bus.StationsQueue)
                            {
                                if (link.To == person.Location)
                                {
                                    break;
                                }
                                else if (link.To == to_station)
                                {
                                    person.SetLocation(bus);
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (person.Location is Bus bus)
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

            if (person.Location != destination && !(person.CurrentAction is Moving moving2 && moving2.Destination == destination))
            {
                if (person.Location is Bus bus)
                {
                    if (bus.Station != null)
                    {
                        person.SetLocation(bus.Station);
                        SetAction(person, null);
                    }
                }
                else
                {
                    Link link;
                    try
                    {
                        link = Controller.Instance.Routes[(person.Location, destination)].Link;
                    }
                    catch (Exception e)
                    {
                        throw new InvalidDataException($"Route between {person.Location} and {destination} required");
                    }

                    SetAction(person, new Moving(link, destination));
                }
            }
        }

        public virtual void Setup(Person person)
        {
            
        }

        public virtual EntityAction SetAction(Person person, EntityAction action)
        {
            person.CurrentAction = action;
            return action;
        }
    }
}
