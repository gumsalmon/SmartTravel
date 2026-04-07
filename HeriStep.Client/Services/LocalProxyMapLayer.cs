using BruTile.Predefined;
using BruTile.Web;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;

namespace HeriStep.Client.Services
{
    /// <summary>
    /// Creates the base map tile layer for the MapPage.
    /// Strategy:
    ///   - Android Emulator: routes through our local API proxy (10.0.2.2:5297)
    ///     so the emulator can reach the internet via the host machine.
    ///   - Fallback: loads tiles directly from OpenStreetMap (requires internet).
    /// </summary>
    public static class LocalProxyMapLayer
    {
        private const string ProxyBaseUrl = "http://10.0.2.2:5297/api/map-tile";

        /// <summary>
        /// Creates a TileLayer using our proxy. If the proxy URL template
        /// is invalid for any reason, falls back to Mapsui's built-in OSM layer.
        /// </summary>
        public static TileLayer Create()
        {
            try
            {
                var tileSource = new HttpTileSource(
                    new GlobalSphericalMercator(),
                    ProxyBaseUrl + "/{z}/{x}/{y}",
                    name: "HeriStep-ProxyOSM",
                    attribution: new BruTile.Attribution(
                        "© OpenStreetMap contributors",
                        "https://www.openstreetmap.org/copyright")
                );

                return new TileLayer(tileSource) { Name = "BaseMapLayer" };
            }
            catch
            {
                // Fallback: use Mapsui's built-in OpenStreetMap layer directly
                return OpenStreetMap.CreateTileLayer();
            }
        }

        /// <summary>
        /// Creates a direct OSM tile layer (no proxy). Useful when running
        /// on a physical device or Windows where 10.0.2.2 is not available.
        /// </summary>
        public static TileLayer CreateDirect()
        {
            return OpenStreetMap.CreateTileLayer();
        }
    }
}
