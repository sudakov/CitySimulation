using Newtonsoft.Json;
using System.ComponentModel;

namespace SimulationCrossplatform
{
    public class DrawJsonConfig
    {
        [DefaultValue("tiles")]
        [JsonProperty("tiles_directory", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string TilesDirectory { get; set; }
        [DefaultValue(8)]
        [JsonProperty("tiles_render_distance", DefaultValueHandling = DefaultValueHandling.Populate)]
        public int TilesRenderDistance { get; set; }
        [DefaultValue(15)]
        [JsonProperty("zoom_close", DefaultValueHandling = DefaultValueHandling.Populate)]
        public int ZoomClose { get; set; }
        [DefaultValue(12)]
        [JsonProperty("zoom_far", DefaultValueHandling = DefaultValueHandling.Populate)]
        public int ZoomFar { get; set; }

        [DefaultValue(0.1f)]
        [JsonProperty("plot_scale", DefaultValueHandling = DefaultValueHandling.Populate)]
        public double PlotScale { get; set; }
        [DefaultValue(0.1)]
        [JsonProperty("plot_step", DefaultValueHandling = DefaultValueHandling.Populate)]
        public double PlotStep { get; set; }
        [DefaultValue(0.1)]
        [JsonProperty("plot_redraw_step", DefaultValueHandling = DefaultValueHandling.Populate)]
        public double PlotRedrawStep { get; set; }
    }
}
