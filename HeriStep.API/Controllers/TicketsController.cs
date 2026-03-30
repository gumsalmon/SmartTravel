using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public TicketsController(HeriStepDbContext context) => _context = context;

        /// <summary>
        /// Validate mã vé của khách du lịch
        /// App Client gọi endpoint này khi khách nhập mã vé tại màn hình Login
        /// </summary>
        [HttpGet("validate/{code}")]
        public async Task<IActionResult> ValidateTicket(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { valid = false, message = "Mã vé không hợp lệ" });

            var ticket = await _context.TouristTickets
                .FirstOrDefaultAsync(t => t.TicketCode == code.ToUpper());

            if (ticket == null)
                return NotFound(new { valid = false, message = "Mã vé không tồn tại trong hệ thống" });

            if (ticket.ExpiryDate < DateTime.Now)
                return Ok(new
                {
                    valid = false,
                    message = $"Vé đã hết hạn vào ngày {ticket.ExpiryDate:dd/MM/yyyy HH:mm}",
                    expiredAt = ticket.ExpiryDate
                });

            return Ok(new
            {
                valid = true,
                message = "Vé hợp lệ! Chào mừng đến Phố Ẩm Thực Vĩnh Khánh 🎉",
                ticket = new
                {
                    code = ticket.TicketCode,
                    expiresAt = ticket.ExpiryDate,
                    packageName = ticket.PackageName
                }
            });
        }

        /// <summary>
        /// Lấy danh sách vé (dùng cho Admin kiểm tra)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tickets = await _context.TouristTickets
                .OrderByDescending(t => t.CreatedAt)
                .Take(50)
                .Select(t => new
                {
                    t.Id,
                    t.TicketCode,
                    t.PackageName,
                    t.AmountPaid,
                    t.CreatedAt,
                    t.ExpiryDate,
                    IsExpired = t.ExpiryDate < DateTime.Now
                })
                .ToListAsync();

            return Ok(tickets);
        }
    }
}
