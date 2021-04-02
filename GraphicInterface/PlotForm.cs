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
        private Dictionary<string, List<(int, int)>> data;
        public PlotForm(Dictionary<string, List<(int, int)>> data)
        {
            InitializeComponent();
            this.data = data;
            comboBox1.Items.AddRange(data.Keys.ToArray());
            if (data.Any())
            {
                comboBox1.SelectedIndex = 0;
            }

        }

        public PlotForm(List<(int, int)> data) : this(new Dictionary<string, List<(int, int)>>(){{"Default", data}})
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
                formsPlot1.plt.PlotSignalXY(
                    points.Select(x=>new DateTime(DateTime.Now.Year, 1,1).AddMinutes(x.Item1).ToOADate()).ToArray(), 
                    points.Select(x => (double)x.Item2).ToArray()
                    );


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
