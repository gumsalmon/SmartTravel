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

        // 1. Lấy danh sách tất cả các điểm (GET: api/Points)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PointOfInterest>>> GetPoints()
        {
            return await _context.Points.ToListAsync();
        }

        // 2. Thêm một điểm mới (POST: api/Points)
        [HttpPost]
        public async Task<ActionResult<PointOfInterest>> PostPoint(PointOfInterest point)
        {
            _context.Points.Add(point);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPoints), new { id = point.Id }, point);
        }
    }
}