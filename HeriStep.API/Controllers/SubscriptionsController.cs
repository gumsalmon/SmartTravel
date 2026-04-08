using HeriStep.Shared.Models.DTOs.Requests;
using HeriStep.Shared.Models.DTOs.Responses;
using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public SubscriptionsController(HeriStepDbContext context) => _context = context;

        /* ========================================================== */
        /* 1. LẤY DANH SÁCH GÓI CƯỚC THIẾT BỊ (CÓ TỰ ĐỘNG DỌN DẸP)     */
        /* ========================================================== */
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Subscription>>> GetSubscriptions()
        {
            // 💡 TỐI ƯU: Dùng .Include(s => s.Stall) để lấy thông tin Sạp đi kèm ngay lập tức
            // Tránh việc gọi FindAsync trong vòng lặp gây chậm hệ thống (Lỗi N+1)
            var subs = await _context.Subscriptions
                .Include(s => s.Stall)
                .ToListAsync();

            bool needUpdateDb = false;
            var now = DateTime.Now;

            // 💡 THUẬT TOÁN TỰ CHỮA LÀNH (SELF-HEALING)
            foreach (var s in subs)
            {
                // ✅ FIX LỖI: Sử dụng "s.IsActive == true" để xử lý kiểu bool? (Nullable)
                // Phép so sánh này đảm bảo kết quả trả về là bool thuần túy để dùng với toán tử &&
                if (s.IsActive == true && s.ExpiryDate.HasValue && s.ExpiryDate.Value < now)
                {
                    // 1. Ép gói cước về trạng thái ngưng hoạt động
                    s.IsActive = false;

                    // 2. Cưỡng chế Đóng cửa sạp liên kết (nếu có)
                    if (s.Stall != null)
                    {
                        s.Stall.IsOpen = false;
                    }

                    needUpdateDb = true;
                }
            }

            // Lưu lại những thay đổi một lần duy nhất để tối ưu database
            if (needUpdateDb)
            {
                await _context.SaveChangesAsync();
            }

            return Ok(subs);
        }

        /* ========================================================== */
        /* 2. KÍCH HOẠT THIẾT BỊ MỚI (Dùng cho Admin)                 */
        /* ========================================================== */
        [HttpPost]
        public async Task<ActionResult<Subscription>> PostSubscription(Subscription sub)
        {
            if (sub == null) return BadRequest("Dữ liệu không hợp lệ.");

            /* 1. Kiểm tra xem ActivationCode đã tồn tại chưa */
            var isCodeUsed = await _context.Subscriptions
                .AnyAsync(s => s.ActivationCode == sub.ActivationCode);

            if (isCodeUsed)
            {
                return BadRequest("⚠️ Mã kích hoạt này đã được sử dụng!");
            }

            /* 2. Tự động tính toán ngày (Server-side side để đảm bảo chính xác) */
            sub.StartDate = DateTime.Now;
            sub.ExpiryDate = DateTime.Now.AddDays(30); // Mặc định gói 30 ngày
            sub.IsActive = true;

            _context.Subscriptions.Add(sub);
            await _context.SaveChangesAsync();

            return Ok(sub);
        }

        /* ========================================================== */
        /* 3. CHI TIẾT GÓI THEO DEVICE ID (Dùng cho App/Client)       */
        /* ========================================================== */
        [HttpGet("device/{deviceId}")]
        public async Task<ActionResult<Subscription>> GetByDeviceId(string deviceId)
        {
            var sub = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.DeviceId == deviceId);

            if (sub == null) return NotFound("Không tìm thấy thiết bị.");

            return Ok(sub);
        }
    }
}
