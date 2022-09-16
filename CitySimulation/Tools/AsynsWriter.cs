using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CitySimulation.Tools
{
    public class AsyncWriter : IDisposable
    {
        private FileStream stream;
        private ConcurrentQueue<string> lines = new ConcurrentQueue<string>();
        private bool isRunning;
        // private bool printConsole;
        public AsyncWriter(string filename)
        {
            // this.printConsole = printConsole;
            if (filename != null)
            {
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
                stream = File.OpenWrite(filename);
            }
            isRunning = true;
            Task.Run(() =>
            {
                while (isRunning)
                {
                   WriteLines();
                   Thread.Sleep(10);
                }
            });
        }

        private void WriteLines()
        {
            lock (stream)
            {
                while (lines.Count > 0)
                {
                    if (lines.TryDequeue(out var line))
                    {
                        // if (printConsole)
                        // {
                        //     Console.WriteLine(line);
                        // }

                        if (stream != null)
                        {
                            stream.Write(Encoding.UTF8.GetBytes(line + '\n'));
                        }
                    }
                }
            }

        }

        public void AddLine(string line)
        {
            lines.Enqueue(line);
        }

        public void AddLines(List<string> list)
        {
            list.ForEach(lines.Enqueue);
        }

        public void Flush()
        {
            WriteLines();
        }

        public void Close()
        {
            WriteLines();

            if (stream.CanWrite)
            {
                stream.Flush(true);
            }

            stream.Close();
            isRunning = false;
        }

        public void Dispose()
        {
            Close();
        }
    }
}
