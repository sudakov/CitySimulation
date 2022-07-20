using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Shapes;

namespace CitySimulation.Tools
{
    public static class OsmTools
    {
        public static BoundingBox TileToBoundingBox(int x, int y, int zoom)
        {
            BoundingBox bb = new BoundingBox();
            bb.North = TileYToLat(y, zoom);
            bb.South = TileYToLat(y + 1, zoom);
            bb.West = TileXToLong(x, zoom);
            bb.East = TileXToLong(x + 1, zoom);

            return bb;
        }

        public static double TileXToLong(int x, int z)
        {
            return x / (double)(1 << z) * 360.0 - 180;
        }

        public static double TileYToLat(int y, int z)
        {
            double n = Math.PI - 2.0 * Math.PI * y / (double)(1 << z);
            return 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
        }

        public static int LongToTileX(double lon, int z)
        {
            return (int)(Math.Floor((lon + 180.0) / 360.0 * (1 << z)));
        }

        public static int LatToTileY(double lat, int z)
        {
            return (int)Math.Floor((1 - Math.Log(Math.Tan(ToRadians(lat)) + 1 / Math.Cos(ToRadians(lat))) / Math.PI) / 2 * (1 << z));
        }

        public static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        public static double ToDegrees(double rad)
        {
            return rad * 180 / Math.PI;
        }
    }
}
