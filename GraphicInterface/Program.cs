using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CitySimulation;
using CitySimulation.Behaviour;
using CitySimulation.Tools;

namespace GraphicInterface
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string filename = args.Length < 1 ? "UPDESUA.json" : args[0];
            if (!File.Exists(filename))
            {
                MessageBox.Show("Invalid arguments. Provide path to configuration file.", "Error", MessageBoxButtons.OK);
                return;
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(filename));
        }
    }
}
