using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using CitySimulation.Control;
using CitySimulation.Tools;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace CitySimulation.Ver2.Control
{
    public class PersonsCounterModule : Module
    {
        public string Filename;
        private AsyncWriter asyncWriter;

        public int LogDeltaTime = 24 * 60;

        private int nextLogTime = -1;
        private List<List<int>> personsCount = new List<List<int>>();

        public override void Setup(Controller controller)
        {
            base.Setup(controller);
            if (!(controller is ControllerSimple))
            {
                throw new Exception("ControllerSimple expected");
            }

            nextLogTime = LogDeltaTime;

            for (int i = 0; i < controller.City.Facilities.Count; i++)
            {
                personsCount.Add(new List<int>());
            }

            if (Filename != null)
            {
                asyncWriter = new AsyncWriter(Filename);
                asyncWriter.AddLine(string.Join(';', "Время", "Тип локации", "ID локации", "Среднее число людей"));
            }
        }

        public override void PreProcess()
        {
            var facilities = Controller.City.Facilities.GetList();
            for (int i = 0; i < facilities.Count; i++)
            {
                personsCount[i].Add(facilities[i].PersonsCount);
            }

            int totalMinutes = Controller.Context.CurrentTime.TotalMinutes;

            if (nextLogTime < totalMinutes)
            {
                LogAll();

                nextLogTime += LogDeltaTime;
            }
        }

        private void LogAll()
        {
            var facilities = Controller.City.Facilities.GetList();

            for (int i = 0; i < facilities.Count; i++)
            {
                List<string> data = new List<string>();
                data.Add(Controller.Context.CurrentTime.ToString());
                data.Add(facilities[i].Type);
                data.Add(facilities[i].Id.ToString());

                double val = (double)personsCount[i].Sum() * Controller.DeltaTime / LogDeltaTime;

                data.Add(val.ToString("F"));

                asyncWriter.AddLine(string.Join(';', data));

                personsCount[i].Clear();
            }
        }

        public override void Finish()
        {
            asyncWriter?.Close();
        }
    }
}
