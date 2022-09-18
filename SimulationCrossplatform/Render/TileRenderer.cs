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
using System.IO;
using DocumentFormat.OpenXml.Wordprocessing;

namespace SimulationCrossplatform.Render
{
    public class TileRenderer
    {
        private Dictionary<(int zoom, int x, int y), (IImage, Rect)?> _tiles = new();
        private Task _tileUpdateTask = Task.CompletedTask;

        private static int SCALE => OsmModel.SCALE;

        private const int MAX_AREA = 15;
        private const string TILE_FILE_FORMAT = "tile_{0}_{1}_{2}.jpeg";


        public string TilesDirectory;
        public int VisibleArea = 10;
        public int ZoomClose = 15;
        public int ZoomFar = 12;
        private int _zoom;

        public TileRenderer()
        {
            _zoom = ZoomClose;
        }

        public void Render(DrawingContext context, Point mapPoint, Action invalidateAction, double scale)
        {
            _zoom = scale > 0.1 ? ZoomClose : ZoomFar;

            var lon_deg = mapPoint.X / SCALE;
            var lat_deg = mapPoint.Y / SCALE;

            var baseTileX = OsmTools.LongToTileX(lon_deg, _zoom);
            var baseTileY = OsmTools.LatToTileY(lat_deg, _zoom);

            int visibleRange = VisibleArea;

            for (int i = -visibleRange; i <= visibleRange; i++)
            {
                for (int j = -visibleRange; j <= visibleRange; j++)
                {
                    int tileX = baseTileX + i;
                    int tileY = baseTileY + j;
                    if (_tiles.TryGetValue((_zoom, tileX, tileY), out var value) && value.HasValue)
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
                _tileUpdateTask = Task.Run(async () =>
                {
                    int layer = 1, leg = 0, x = 0, y = 0;
                    int zoom = _zoom;

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

                        if (lastPoint != point || _zoom != zoom)
                        {
                            zoom = _zoom;
                            lastPoint = point;
                            layer = 1;
                            leg = 0;
                            x = 0;
                            y = 0;
                        }

                        if (layer < MAX_AREA)
                        {
                            LoadTile(point, _zoom, x, y, invalidateAction);

                            goNext();
                        }
                        else
                        {
                            await Task.Delay(100);
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

        private void LoadTile(Point center, int zoom, int x, int y, Action invalidateAction)
        {
            var lon_deg = center.X / SCALE;
            var lat_deg = center.Y / SCALE;
            var tileX = OsmTools.LongToTileX(lon_deg, zoom) + x;
            var tileY = OsmTools.LatToTileY(lat_deg, zoom) + y;

            if (!_tiles.ContainsKey((zoom, tileX, tileY)))
            {
                _tiles.Add((zoom, tileX, tileY), null);

                BoundingBox bb = OsmTools.TileToBoundingBox(tileX, tileY, zoom);
                var r = new Rect(bb.West, -bb.North, bb.East - bb.West, bb.North - bb.South) * SCALE;
                Bitmap tile = GetTile(zoom, tileX, tileY);
                _tiles[(zoom, tileX, tileY)] = (tile, r);

                if (TilesDirectory != null)
                {
                    if (!Directory.Exists(TilesDirectory))
                    {
                        Directory.CreateDirectory(TilesDirectory);
                    }

                    tile.Save(Path.Combine(TilesDirectory, string.Format(TILE_FILE_FORMAT, zoom, tileX, tileY)));
                }

                Dispatcher.UIThread.InvokeAsync(invalidateAction);
            }
        }

        private Bitmap GetTile(int zoom, int tileX, int tileY)
        {
            if (TilesDirectory != null)
            {
                string filename = Path.Combine(TilesDirectory, string.Format(TILE_FILE_FORMAT, zoom, tileX, tileY));

                if (File.Exists(filename))
                {
                    try
                    {
                        return new Bitmap(filename);
                    }
                    catch (Exception)
                    {
                        File.Delete(filename);
                    }
                }
            }

            return DownloadTile(tileX, tileY, zoom);
        }
    }
}
