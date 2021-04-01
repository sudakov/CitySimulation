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
        private List<(int, int)> data;
        public PlotForm(List<(int, int)>  data)
        {
            InitializeComponent();
            this.data = data;
            // formsPlot1.plt.PlotScatter(data.Select(x => (double)x.Item1).ToArray(), data.Select(x => (double)x.Item2).ToArray());
            var a1 = data.Select(x => (double) x.Item1).ToArray();
            var a2 = data.Select(x => (double) x.Item2).ToArray();
            formsPlot1.plt.PlotSignalXY(a1, a2);
        }

        private void formsPlot1_Load(object sender, EventArgs e)
        {

        }
    }
}
