using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Entities;
using CitySimulation.Entity;
using CitySimulation.Navigation;
using CitySimulation.Tools;

namespace CitySimulation.Behaviour.Action
{
    public class CarMoving : Moving
    {
        public Car Car;

        public CarMoving(Car car, Facility from, Facility destination) : base(new Link(from, destination, Point.Distance(from.Coords, destination.Coords)), destination)
        {
            Car = car;
        }
        

        public override string ToString()
        {
            return $"Car: {DistanceCovered} ({Destination})";
        }
    }
}
