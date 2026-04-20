using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        double minLat = 10.745, maxLat = 10.775;
        double minLon = 106.685, maxLon = 106.720;
        int[] zooms = { 15, 16, 17, 18 };
        
        string baseDir = @"C:\Users\Admin\source\repos\SmartTravel1-master\SmartTravel-master\HeriStep.Client\Resources\Raw\leaflet\tiles";
        
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "SmartTravelApp-LocalCache");

        int count = 0;
        foreach (var z in zooms)
        {
            var p1 = DegToNum(minLat, minLon, z);
            var p2 = DegToNum(maxLat, maxLon, z);
            
            int xmin = Math.Min(p1.X, p2.X);
            int xmax = Math.Max(p1.X, p2.X);
            int ymin = Math.Min(p1.Y, p2.Y);
            int ymax = Math.Max(p1.Y, p2.Y);

            for (int x = xmin; x <= xmax; x++)
            {
                for (int y = ymin; y <= ymax; y++)
                {
                    // Chuyển sang Voyager style cho màu sắc tươi sáng
                    string url = $"https://a.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}.png";
                    string dirInfo = Path.Combine(baseDir, z.ToString(), x.ToString());
                    Directory.CreateDirectory(dirInfo);
                    string finalFile = Path.Combine(dirInfo, $"{y}.png");
                    
                    if (File.Exists(finalFile)) continue;

                    try {
                        var bytes = await http.GetByteArrayAsync(url);
                        await File.WriteAllBytesAsync(finalFile, bytes);
                        count++;
                        if (count % 10 == 0) Console.WriteLine($"Downloaded {count} voyager tiles...");
                        await Task.Delay(50);
                    } catch(Exception e) {
                        Console.WriteLine($"Error {url}: {e.Message}");
                    }
                }
            }
        }
        Console.WriteLine($"Finished downloading {count} tiles.");
    }
    
    static (int X, int Y) DegToNum(double lat, double lon, int zoom)
    {
        int n = 1 << zoom;
        double latRad = lat * Math.PI / 180.0;
        int x = (int)((lon + 180.0) / 360.0 * n);
        int y = (int)((1.0 - Math.Asinh(Math.Tan(latRad)) / Math.PI) / 2.0 * n);
        return (x, y);
    }
}
