using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeriStep.API.Data;
using HeriStep.Shared;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PointsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public PointsController(HeriStepDbContext context)
        {
            _context = context;
        }

        // 1. Đổi tên thành GetPoints để khớp với nameof() bên dưới
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PointOfInterest>>> GetPoints()
        {
            // Đảm bảo trong DbContext bạn đã đặt tên DbSet là Stalls
            return await _context.Stalls.ToListAsync();
        }

        // 2. Thêm một điểm mới
        [HttpPost]
        public async Task<ActionResult<PointOfInterest>> PostPoint(PointOfInterest point)
        {
            _context.Stalls.Add(point);
            await _context.SaveChangesAsync();

            // nameof(GetPoints) bây giờ đã hợp lệ vì phương thức trên đã đổi tên
            return CreatedAtAction(nameof(GetPoints), new { id = point.Id }, point);
        }
    }
}