using HeriStep.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Dòng này sẽ giải quyết toàn bộ lỗi Async
[Route("api/[controller]")]
[ApiController]
public class SubscriptionsController : ControllerBase
{
    private readonly HeriStepDbContext _context;
    public SubscriptionsController(HeriStepDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Subscription>>> GetSubscriptions()
    {
        // Join với bảng Stalls để lấy tên sạp hiển thị cho Admin
        var subs = await (from sub in _context.Subscriptions
                          join s in _context.Stalls on sub.StallId equals s.Id
                          select new Subscription
                          {
                              Id = sub.Id,
                              StallId = sub.StallId,
                              StallName = s.Name,
                              PlanName = sub.PlanName,
                              EndDate = sub.EndDate,
                              IsActive = sub.EndDate > DateTime.Now
                          }).ToListAsync();
        return subs;
    }
}