using ScottPlot;
using ScottPlot.Avalonia;
using System;
using Color = System.Drawing.Color;

namespace SimulationCrossplatform.Controls
{

    public class RealtimePlot : AvaPlot
    {
        public double Scale = 0.1f;
        public int Step = 100;
        public int RenderStep = 100;

        private (double time, double value)? _lastPoint;
        private int? _lastTime;
        private int? _lastRedrawTime;
        private double _firstTime;
        private double _max = 0;

        public RealtimePlot()
        {
            Plot.XAxis.DateTimeFormat(true);
        }

        public void AddPoint((int minutes, double value) point)
        {
            (double time, double value) newPoint = (new DateTime(DateTime.Now.Year, 1, 1).AddMinutes(point.minutes).ToOADate(), point.value);

            bool redrawFlag = false;

            if (_lastPoint.HasValue)
            {
                if (_lastTime.HasValue && point.minutes > _lastTime.Value + Step)
                {
                    Plot.AddLine(_lastPoint.Value.time, _lastPoint.Value.value, newPoint.time, newPoint.value, Color.Red);

                    _lastPoint = newPoint;
                    _lastTime = point.minutes;

                    if (_firstTime + (35 * Scale) < newPoint.time)
                    {
                        Plot.RemoveAt(0);
                    }

                    if (newPoint.value > _max)
                    {
                        _max = newPoint.value;
                    }

                    redrawFlag = true;
                }
            }
            else
            {
                _firstTime = newPoint.time;
                _lastPoint = newPoint;
                _lastTime = point.minutes;
                _lastRedrawTime = point.minutes;
            }


            if (redrawFlag || _lastRedrawTime.HasValue && point.minutes > _lastRedrawTime.Value + RenderStep)
            {
                _lastRedrawTime = point.minutes;
                Plot.SetAxisLimits(newPoint.time - (30 * Scale), newPoint.time + (5 * Scale), 0, _max * 1.2);
                Render();
            }
        }
    }
}
