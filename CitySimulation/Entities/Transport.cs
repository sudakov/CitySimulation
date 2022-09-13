using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CitySimulation.Behaviour.Action;
using CitySimulation.Navigation;
using CitySimulation.Tools;

namespace CitySimulation.Entities
{
    public class Transport : Facility
    {
        private List<Station> route;

        public Station Station;
        public Queue<Link> StationsQueue = new Queue<Link>();
        public EntityAction Action;

        public int Delay = 1;
        public int Capacity = 30;
        public int Speed = 10;


        private int _compensation = 0;

        private Dictionary<Facility, Station> _closestStations = new Dictionary<Facility, Station>();


        public Transport(string name, List<Station> route) : base(name)
        {
            this.route = route;
            Type = "Bus";
        }

        public bool HavePlace => PersonsCount < Capacity;

        public void SetupRoute(RouteTable routeTable, IEnumerable<Facility> facilities)
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
                    //throw new Exception("Incorrect route for station");
                    // route.RemoveAll(x => x == r2);
                    // i--;
                    // continue;
                    var dist = Point.Distance(r1.Coords, r2.Coords);
                    var link = new Link(r1, r2, dist, dist/5);
                    pathSegment = new PathSegment(link, link.Length, link.Time);
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


            foreach (var facility in facilities.Where(x=>!(x is Transport)))
            {
                if (facility is Station station && StationsQueue.Any(x=>x.To == station || x.From == station))
                {
                    _closestStations.Add(station, station);
                }
                else
                {
                    Link min = StationsQueue.Where(x => x.To != facility).MinBy(x => routeTable.GetValueOrDefault((x.To, facility), null)?.TotalTime ?? double.PositiveInfinity);
                    _closestStations.Add(facility, (Station)min.To);
                }
            }
        }

        public List<Station> GetRoute()
        {
            return new List<Station>(route);
        }
        public Station GetClosest(Facility facility)
        {
            var closest = _closestStations.GetValueOrDefault(facility, null);
            foreach (var link in StationsQueue)
            {
                if (link.To == closest)
                {
                    return closest;
                }
                else if(link.To == Station)
                {
                    return null;
                }
            }

            return null;
        }

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
            Station = route.Skip(count).First();

            return this;
        }
        public Facility SetRandomStation()
        {
            int rand = Controller.Random.Next(0, route.Count);
            SkipStations(rand);

            return this;
        }

        public override Point? CalcCoords()
        {
            if (Station != null)
            {
                return Station?.CalcCoords();
            }
            else if(Action is Moving moving)
            {
                double k = moving.DistanceCovered / moving.Link.Length;

                return (1 - k) * moving.Link.From.Coords + k * moving.Link.To.Coords;
            }

            return null;
        }
    }
}
