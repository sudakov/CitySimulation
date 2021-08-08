using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CitySimulation.Entity;
using CitySimulation.Tools;

namespace CitySimulation.Control.Log
{
    public class FileLogger : Logger
    {
        private ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
        private AutoResetEvent enqueueEvent = new AutoResetEvent(false);
        private StreamWriter writer;
        private FileStream fileStream;
        public override void LogPersonInFacilityTime(LogCityTime start, LogCityTime end, Facility facility, Person person)
        {
            queue.Enqueue(start + " - " + end + ": " + facility + " -> " + person);
            // enqueueEvent.Set();
        }

        public override int? Start()
        {
            fileStream = new FileStream("visit_log.txt", FileMode.Create);
            writer = new StreamWriter(fileStream);

            Task.Run(() =>
            {
                while (Controller.IsRunning)
                {
                    // if (queue.IsEmpty)
                    // {
                    //     enqueueEvent.WaitOne();
                    // }

                    while (queue.Any())
                    {
                        queue.TryDequeue(out string line);

                        writer.WriteLine(line);
                    }
                }
                writer.Flush();
                writer.Close();
            });
            
            return null;
        }

        public override void Stop()
        {
            writer.Flush();
            writer.Close();
        }

        public override void LogVisit(Service service)
        {
            
        }
    }
}
