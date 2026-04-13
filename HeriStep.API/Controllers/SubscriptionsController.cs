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
        /* 3. CHI TIẾT GÓI THEO DEVICE ID (Dùng cho App/Client để double-check chống time-hack) */
        /* ========================================================== */
        [HttpGet("device/{deviceId}")]
        public async Task<ActionResult> GetByDeviceId(string deviceId)
        {
            // Tìm ticket trong TouristTickets (luồng App Client)
            var ticket = await _context.TouristTickets
                .Where(t => t.DeviceId == deviceId)
                .OrderByDescending(t => t.ExpiryDate)
                .FirstOrDefaultAsync();

            var now = DateTime.Now;

            if (ticket != null)
            {
                bool isExpired = ticket.ExpiryDate < now;
                return Ok(new
                {
                    IsExpired = isExpired,
                    IsActive = !isExpired,
                    ServerTime = now,          // Server time — chống time-hack
                    ExpiryDate = ticket.ExpiryDate,
                    PackageId = ticket.PackageId
                });
            }

            // Tìm trong Subscriptions (luồng Merchant/Stall Owner)
            var sub = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.DeviceId == deviceId);

            if (sub != null)
            {
                bool isExpired = sub.IsActive != true ||
                                 (sub.ExpiryDate.HasValue && sub.ExpiryDate.Value < now);
                return Ok(new
                {
                    IsExpired = isExpired,
                    IsActive = !isExpired,
                    ServerTime = now,
                    ExpiryDate = sub.ExpiryDate,
                    PackageId = (int?)null
                });
            }

            // Thiết bị chưa đăng ký
            return Ok(new
            {
                IsExpired = true,
                IsActive = false,
                ServerTime = now,
                ExpiryDate = (DateTime?)null,
                PackageId = (int?)null
            });
        }
    }
}
