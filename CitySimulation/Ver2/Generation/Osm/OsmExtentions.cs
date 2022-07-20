using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsmSharp.Tags;

namespace CitySimulation.Ver2.Generation.Osm
{
    public static class OsmExtentions
    {
        public static string GetOrDefault(this TagsCollectionBase self, string key, string defaultValue = null) 
            => self.ContainsKey(key) ? self[key] : defaultValue;
    }
}
