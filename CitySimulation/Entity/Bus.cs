using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Navigation;

namespace CitySimulation.Entity
{
    public class Bus : Facility
    {
        private List<Station> route;

        public Station Station;
        public Queue<Link> StationsQueue = new Queue<Link>();
        public EntityAction Action;

        public int Delay = 1;
        public int Capacity = 30;
        public int Speed = 10;


        private int _compensation = 0;

        public Bus(string name, List<Station> route) : base(name)
        {
            this.route = route;
        }

        public bool HavePlace => PersonsCount < Capacity;

        public void SetupRoute(RouteTable routeTable)
        {
            for (int i = 0; i < route.Count; i++)
            {
                Station r1, r2;
                if (i != route.Count - 1)
                {
                    r1 = route[i];
                    r2 = route[i + 1];
                }
                else
                {
                    r1 = route[i];
                    r2 = route[0];
                }

                PathSegment pathSegment = routeTable[(r1, r2)];

                if (pathSegment.Link.To != r2)
                {
                    throw new Exception("Incorrect route for station");
                }

                StationsQueue.Enqueue(pathSegment.Link);
            }

            if (Station == null)
            {
                Station = route[0];
            }
            else if(StationsQueue.Any(x=>x.From == Station))
            {
                while (StationsQueue.Peek().From != Station)
                {
                    StationsQueue.Enqueue(StationsQueue.Dequeue());
                }
            }
            else
            {
                throw new Exception("Station not found in the route");
            }
        }

        // public override bool AddPerson(Person person)
        // {
        //     if (PersonsCount >= Capacity)
        //     {
        //         return false;
        //     }
        //
        //     return base.AddPerson(person);
        // }

        public override void PreProcess()
        {
            base.PreProcess();

            int deltaTime = Controller.Instance.DeltaTime;
            if (Action is Moving moving)
            {
                moving.DistanceCovered += deltaTime * Speed;

                moving.DistanceCovered += _compensation;

                if (moving.DistanceCovered >= moving.Link.Length)
                {
                    _compensation = moving.DistanceCovered - (int)moving.Link.Length;

                    StationsQueue.Enqueue(moving.Link);
                    Station = (Station)moving.Link.To;
                    Station.Buses.AddFirst(this);

                    Action = new Waiting(Delay);
                }
            }
            else if (Action is Waiting waiting)
            {
                waiting.RemainingTime -= deltaTime;
                if (waiting.RemainingTime <= 0)
                {
                    Link station = StationsQueue.Dequeue();
                    Station.Buses.Remove(this);
                    Station = null;

                    Action = new Moving(station, station.To);
                }
            }
            else if (Station != null)
            {
                Action = new Waiting(Delay);
            }
        }

        public Facility SkipStations(int count)
        {
            Station = (Station)route.Skip(count).First();

            return this;
        }
        public Facility SetRandomStation()
        {
            int rand = Controller.Random.Next(0, route.Count);
            SkipStations(rand);

            return this;
        }
    }
}
