using Avalonia;
using CitySimulation.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using Point = Avalonia.Point;
using Avalonia.Threading;
using CitySimulation.Ver2.Generation.Osm;

namespace SimulationCrossplatform.Render
{
    public class TileRenderer
    {
        private Dictionary<(int, int), (IImage, Rect)?> _tiles = new();
        private Task _tileUpdateTask = Task.CompletedTask;

        private static int SCALE => OsmModel.SCALE;
        private const int ZOOM = 15;

        private const int MAX_AREA = 5;
        private const int VISIBLE_AREA = 20;


        public void Render(DrawingContext context, Point point, Action invalidateAction)
        {
            var lon_deg = point.X / SCALE;
            var lat_deg = point.Y / SCALE;

            var baseTileX = OsmTools.LongToTileX(lon_deg, ZOOM);
            var baseTileY = OsmTools.LatToTileY(lat_deg, ZOOM);

            for (int i = -VISIBLE_AREA; i <= VISIBLE_AREA; i++)
            {
                for (int j = -VISIBLE_AREA; j <= VISIBLE_AREA; j++)
                {
                    int tileX = baseTileX + i;
                    int tileY = baseTileY + j;
                    if (_tiles.TryGetValue((tileX, tileY), out var value) && value.HasValue)
                    {
                        var (image, rect) = value.Value;
                        context.DrawImage(image, rect.MapToScreen());
                    }
                }
            }
        }

        public void RunLoadTask(Func<Point> pointFunc, Action invalidateAction)
        {
            if (_tileUpdateTask.IsCompleted)
            {
                _tileUpdateTask = Task.Run(() =>
                {
                    int layer = 1, leg = 0, x = 0, y = 0;

                    void goNext()
                    {
                        switch (leg)
                        {
                            case 0: ++x; if (x == layer) ++leg; break;
                            case 1: ++y; if (y == layer) ++leg; break;
                            case 2: --x; if (-x == layer) ++leg; break;
                            case 3: --y; if (-y == layer) { leg = 0; ++layer; } break;
                        }
                    }

                    Point lastPoint = default;
                    while (true)
                    {
                        Point point = -pointFunc().ScreenToMap();

                        if (lastPoint != point)
                        {
                            lastPoint = point;
                            layer = 1;
                            leg = 0;
                            x = 0;
                            y = 0;
                        }

                        if (layer < MAX_AREA)
                        {
                            LoadTile(point, x, y, invalidateAction);

                            goNext();
                        }

                    }
                });
            }
        }


        private Bitmap DownloadTile(int x, int y, int zoom)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(System.Reflection.Assembly.GetEntryAssembly().GetName().Name, System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()));
            var responseMessage = client.GetAsync($"https://b.tile.openstreetmap.org/{zoom}/{x}/{y}.png").Result;
            Bitmap bitmap2 = new Bitmap(responseMessage.Content.ReadAsStream());

            return bitmap2;
        }

        private void LoadTile(Point center, int x, int y, Action invalidateAction)
        {
            var lon_deg = center.X / SCALE;
            var lat_deg = center.Y / SCALE;
            var tileX = OsmTools.LongToTileX(lon_deg, ZOOM) + x;
            var tileY = OsmTools.LatToTileY(lat_deg, ZOOM) + y;

            if (!_tiles.ContainsKey((tileX, tileY)))
            {
                _tiles.Add((tileX, tileY), null);

                BoundingBox bb = OsmTools.TileToBoundingBox(tileX, tileY, ZOOM);
                var r = new Rect(bb.West, -bb.North, bb.East - bb.West, bb.North - bb.South) * SCALE;
                Bitmap tile = DownloadTile(tileX, tileY, ZOOM);
                _tiles[(tileX, tileY)] = (tile, r);

                Dispatcher.UIThread.InvokeAsync(() => { invalidateAction(); });
            }
        }
    }
}
