using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ScottPlot;

namespace GraphicInterface
{
    public partial class PlotForm : Form
    {
        public enum PlotType
        {
            Lines,
            Gistogram
        }

        public PlotType plotType;
        private Dictionary<string, List<(int, int)>> data;
        public PlotForm(Dictionary<string, List<(int, int)>> data, PlotType plotType = PlotType.Lines)
        {
            InitializeComponent();
            this.plotType = plotType;
            this.data = data;
            comboBox1.Items.AddRange(data.Keys.ToArray());
            if (data.Any())
            {
                comboBox1.SelectedIndex = 0;
            }

        }

        public PlotForm(List<(int, int)> data, PlotType plotType = PlotType.Lines) : this(new Dictionary<string, List<(int, int)>>(){{"Default", data}}, plotType)
        {
        }

        private void formsPlot1_Load(object sender, EventArgs e)
        {

        }


        private void Plot()
        {
            string name = comboBox1.SelectedItem?.ToString();
            if (data.ContainsKey(name))
            {
                var points = data[name];

                formsPlot1.plt.Clear();
                if (plotType == PlotType.Lines)
                {
                    formsPlot1.plt.PlotSignalXY(
                        points.Select(x => new DateTime(DateTime.Now.Year, 1, 1).AddMinutes(x.Item1).ToOADate()).ToArray(),
                        points.Select(x => (double)x.Item2).ToArray()
                    );
                }
                else if (plotType == PlotType.Gistogram)
                {
                    formsPlot1.plt.PlotStep(
                        points.Select(x => new DateTime(DateTime.Now.Year, 1, 1).AddMinutes(x.Item1).ToOADate()).ToArray(),
                        points.Select(x => (double)x.Item2).ToArray()
                    );
                }
                


                formsPlot1.plt.Ticks(dateTimeX: true);

                formsPlot1.Update();
                formsPlot1.Render();

            }

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Plot();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            Plot();
        }
    }
}
