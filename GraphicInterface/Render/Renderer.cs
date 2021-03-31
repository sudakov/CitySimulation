using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using CitySimulation.Entity;

namespace GraphicInterface.Render
{
    public abstract class Renderer
    {
        public virtual void Render(Entity facility, Graphics g, RenderParams renderParams) { }

    }
}
