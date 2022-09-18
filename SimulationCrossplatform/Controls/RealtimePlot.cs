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

        private (double time, int value)? _lastPoint;
        private int? _lastTime;
        private int? _lastRedrawTime;
        private double _firstTime;
        private int _max = 0;

        public RealtimePlot()
        {
            Plot.XAxis.DateTimeFormat(true);
        }

        public void AddPoint((int time, int value) point)
        {
            (double time, int value) newPoint = (new DateTime(DateTime.Now.Year, 1, 1).AddMinutes(point.time).ToOADate(), point.value);

            bool redrawFlag = false;

            if (_lastPoint.HasValue)
            {
                if (_lastTime.HasValue && point.time > _lastTime.Value + Step)
                {
                    Plot.AddLine(_lastPoint.Value.time, _lastPoint.Value.value, newPoint.time, newPoint.value, Color.Red);

                    _lastPoint = newPoint;
                    _lastTime = point.time;

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
                _lastTime = point.time;
                _lastRedrawTime = point.time;
            }


            if (redrawFlag || _lastRedrawTime.HasValue && point.time > _lastRedrawTime.Value + RenderStep)
            {
                _lastRedrawTime = point.time;
                Plot.SetAxisLimits(newPoint.time - (30 * Scale), newPoint.time + (5 * Scale), 0, _max * 1.2);
                Render();
            }
        }
    }
}
