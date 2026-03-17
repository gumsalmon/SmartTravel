using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeriStep.API.Data;
using HeriStep.Shared;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StallsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public StallsController(HeriStepDbContext context)
        {
            _context = context;
        }

        // Chức năng: Lấy danh sách tất cả các quán
        // Gọi bằng link: https://localhost.../api/stalls
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Stall>>> GetStalls()
        {
            return await _context.Stalls.ToListAsync();
        }
    }
}