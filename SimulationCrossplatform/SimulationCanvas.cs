using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;

namespace SimulationCrossplatform
{
    public class SimulationCanvas : Control
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();

        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
        }

        public override void Render(DrawingContext context)
        {

        }
    }
}
