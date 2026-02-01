using HeriStep.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class StatsController : ControllerBase
{
    private readonly HeriStepDbContext _context;
    public StatsController(HeriStepDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<DashboardStats>> GetStats()
    {
        return new DashboardStats
        {
            TotalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer"),
            TotalStalls = await _context.Stalls.CountAsync(),
            TotalScans = 8920, // Sau này lấy từ bảng Logs
            TotalRevenue = 24500000 // Sau này lấy từ bảng Payments
        };
    }
}