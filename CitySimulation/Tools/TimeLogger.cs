using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CitySimulation.Tools
{
    public static class TimeLogger
    {
        private static DateTime lastLog = DateTime.Now;
        public static void Log(string str)
        {
            TimeSpan delta = DateTime.Now - lastLog;
            lastLog = DateTime.Now;
            Debug.WriteLine(str + ": " + delta.ToString("g"));
            File.AppendAllText("log.txt", str + ": " + delta.ToString("g"));
        }
    }
}
