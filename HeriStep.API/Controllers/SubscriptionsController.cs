using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class SubscriptionsController : ControllerBase
{
    private readonly HeriStepDbContext _context;
    public SubscriptionsController(HeriStepDbContext context) => _context = context;

    // 1. LẤY DANH SÁCH GÓI CƯỚC THIẾT BỊ
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Subscription>>> GetSubscriptions()
    {
        // Vì DB hiện tại chưa có stall_id trong bảng Subscriptions, 
        // nên chúng ta sẽ lấy danh sách thuần từ bảng Subscriptions.
        return await _context.Subscriptions.ToListAsync();
    }

    // 2. KÍCH HOẠT THIẾT BỊ MỚI (Dùng cho Admin)
    [HttpPost]
    public async Task<ActionResult<Subscription>> PostSubscription(Subscription sub)
    {
        // Kiểm tra xem DeviceId hoặc ActivationCode đã tồn tại chưa
        if (await _context.Subscriptions.AnyAsync(s => s.ActivationCode == sub.ActivationCode))
        {
            return BadRequest("❌ Mã kích hoạt này đã được sử dụng!");
        }

        // Tự động tính toán ngày
        sub.StartDate = DateTime.Now;
        sub.ExpiryDate = DateTime.Now.AddDays(30); // Mặc định 30 ngày
        sub.IsActive = true;

        _context.Subscriptions.Add(sub);
        await _context.SaveChangesAsync();

        return Ok(sub);
    }
}