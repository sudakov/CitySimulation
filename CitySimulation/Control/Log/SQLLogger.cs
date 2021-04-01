using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitySimulation.Control.Log.DbModel;
using CitySimulation.Control.Log.SQL;
using CitySimulation.Entity;
using CitySimulation.Tools;

namespace CitySimulation.Control.Log
{
    public class SQLLogger : Logger
    {
        private EFContext db;
        private ConcurrentBag<(LogCityTime, LogCityTime, Facility, Person)> queue = new ConcurrentBag<(LogCityTime, LogCityTime, Facility, Person)>();

        public SQLLogger()
        {

        }

        public override void LogPersonInFacilityTime(LogCityTime start, LogCityTime end, Facility facility, Person person)
        {
            // db.PersonInFacilityTimes.AddAsync(new PersonInFacilityTime()
            // {
            //     SessionId = SessionId,
            //     StartDay = start.Day,
            //     StartMin = start.Minutes,
            //     EndDay = end.Day,
            //     EndMin = end.Minutes,
            //     Facility = facility.Name,
            //     Person = person.Name
            // });
            lock (queue)
            {
                queue.Add((start, end, facility, person));
            }
        }

        public override int? Start()
        {
            db = new EFContext();
            var session = db.Sessions.Add(new Session(){DateTime = DateTime.Now});
            SessionId = session.Entity.Id;

            Task.Run(() =>
            {
                while (Controller.IsRunning)
                {
                    Flush();
                }
            });

            return SessionId;
        }
        
        public void Flush()
        {
            if (!queue.IsEmpty)
            {
                PersonInFacilityTime[] array;
        
                lock (queue)
                {
                    array = queue.ToArray().Select(item => new PersonInFacilityTime()
                    {
                        SessionId = SessionId,
                        StartDay = item.Item1.Day,
                        StartMin = item.Item1.Minutes,
                        EndDay = item.Item2.Day,
                        EndMin = item.Item2.Minutes,
                        Facility = item.Item3.Name,
                        Person = item.Item4.Name
                    }).ToArray();
                    queue.Clear();
                }
        
                db.PersonInFacilityTimes.AddRangeAsync(array).ContinueWith(task => db.SaveChangesAsync());
            }
        }

        public override void Stop()
        {
            Flush();
            db.SaveChanges();
            db.Dispose();
            SessionId = -1;
            db = null;
        }
    }
}
