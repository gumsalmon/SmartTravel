using Microsoft.AspNetCore.Mvc;

namespace HeriStep.API.Controllers
{
    /// <summary>
    /// MapTileController - Proxy trung gian lấy ảnh tile bản đồ từ OpenStreetMap
    /// Giúp máy ảo Android (không có internet) tải được ảnh đường phố qua đường hầm 10.0.2.2:5297
    /// </summary>
    [Route("api/map-tile")]
    [ApiController]
    public class MapTileController : ControllerBase
    {
        private static readonly HttpClient _tileClient = new HttpClient()
        {
            DefaultRequestHeaders = {
                { "User-Agent", "HeriStepApp/1.0 (sinhvien@sgu.edu.vn)" }
            }
        };

        [HttpGet("{z}/{x}/{y}")]
        public async Task<IActionResult> GetTile(int z, int x, int y)
        {
            try
            {
                // Lấy ảnh từ OpenStreetMap (máy tính thật có internet)
                var url = $"https://tile.openstreetmap.org/{z}/{x}/{y}.png";
                var bytes = await _tileClient.GetByteArrayAsync(url);
                return File(bytes, "image/png");
            }
            catch
            {
                return NotFound();
            }
        }
    }
}
