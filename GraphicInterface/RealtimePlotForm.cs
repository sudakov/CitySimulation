using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace GraphicInterface
{
    public partial class RealtimePlotForm : Form
    {
        public RealtimePlotForm(string title)
        {
            InitializeComponent();
            formsPlot1.plt.Ticks(dateTimeX: true);
            Text = title;
        }

        private (double, int)? lastPoint;
        public void AddPoint((double, int) point)
        {
            if (this.Created)
            {
                this.Invoke(new Action(() =>
                {
                    (double, int) newPoint = (new DateTime(DateTime.Now.Year, 1, 1).AddMinutes(point.Item1).ToOADate(), point.Item2);
                    if (lastPoint.HasValue)
                    {
                        formsPlot1.plt.PlotLine(lastPoint.Value.Item1, lastPoint.Value.Item2, newPoint.Item1, newPoint.Item2, Color.Red);
                        if (auto_checkbox.Checked)
                        {
                            formsPlot1.plt.AxisAuto();
                        }
                        formsPlot1.plt.Axis(newPoint.Item1 - 30, newPoint.Item1 + 5);

                        formsPlot1.Update();
                        formsPlot1.Render();
                    }

                    lastPoint = newPoint;
                }));
            }
        }
    }
}
