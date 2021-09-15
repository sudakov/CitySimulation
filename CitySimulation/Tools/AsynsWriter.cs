using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace CitySimulation.Tools
{
    public class AsyncWriter : IDisposable
    {
        private FileStream stream;
        private Queue<string> lines = new Queue<string>();
        private bool isRunning;
        private bool printConsole;
        public AsyncWriter(string filename, bool printConsole)
        {
            this.printConsole = printConsole;
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
                }
            });
        }

        private void WriteLines()
        {
            lock (stream)
            {
                while (lines.Count > 0)
                {
                    var line = lines.Dequeue();
                    if (printConsole)
                    {
                        Console.WriteLine(line);
                    }

                    if (stream != null)
                    {
                        stream.Write(Encoding.UTF8.GetBytes(line + '\n'));
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
            stream.Flush(true);
            stream.Close();
            isRunning = false;
        }

        public void Dispose()
        {
            Close();
        }
    }
}
