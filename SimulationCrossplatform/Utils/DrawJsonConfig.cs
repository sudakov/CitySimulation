using Newtonsoft.Json;
using System.ComponentModel;

namespace SimulationCrossplatform.Utils
{
    public class DrawJsonConfig
    {
        [DefaultValue("tiles")]
        [JsonProperty("tiles_directory", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string TilesDirectory { get; set; }
        [DefaultValue(8)]
        [JsonProperty("tiles_render_distance", DefaultValueHandling = DefaultValueHandling.Populate)]
        public int TilesRenderDistance { get; set; }
        [DefaultValue(14)]
        [JsonProperty("zoom", DefaultValueHandling = DefaultValueHandling.Populate)]
        public int Zoom { get; set; }
    }
}
