using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CitySimulation.Control.Log.DbModel;
using CitySimulation.Entities;
using CitySimulation.Tools;
using CitySimulation.Ver1.Entity;
using LiteDB;

namespace CitySimulation.Control.Log
{
    public class DBLogger : Logger
    {
        public LiteDatabase Database => db;

        private ConcurrentBag<(LogCityTime, LogCityTime, Facility, Person)> queue = new ConcurrentBag<(LogCityTime, LogCityTime, Facility, Person)>();

        private LiteDatabase db;
        private ILiteCollection<PersonInFacilityTime> personInFacilityCollection;

        public override void LogPersonInFacilityTime(LogCityTime start, LogCityTime end, Facility facility, Person person)
        {
            lock (queue)
            {
                queue.Add((start, end, facility, person));
            }
        }

        public LiteDatabase CreateConnection()
        {
            return new LiteDatabase("city_simulation.db");
        }

        public override int? Start()
        {
            db = CreateConnection();
            personInFacilityCollection = db.GetCollection<PersonInFacilityTime>();

            ILiteCollection<Session> sessionCollection = db.GetCollection<Session>();
            SessionId = sessionCollection.Insert(new Session() {DateTime = DateTime.Now}).AsInt32;

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
                    Debug.WriteLine(queue.Count);
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

                personInFacilityCollection.Insert(array);
            }
        }

        public override void Stop()
        {
            Flush();
            db.Dispose();
            SessionId = -1;
        }

        public override void LogVisit(Service service)
        {
            
        }
    }
}
