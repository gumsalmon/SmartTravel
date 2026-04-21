using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;
        private readonly string _bankName;
        private readonly string _soTaiKhoan;
        private readonly string _accountName;
        private readonly string _pendingTicketsFile = "pending_tickets.json";

        public TicketsController(HeriStepDbContext context, IConfiguration config)
        {
            _context = context;
            _bankName = config["SePay:BankName"] ?? "MBBank";
            _soTaiKhoan = config["SePay:BankAccount"] ?? "0388764276";
            _accountName = config["SePay:AccountName"] ?? "NGO DUC HUY";
        }

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

        // =========================================================
        // SUBSCRIPTION DEVICE-BASED APIs
        // =========================================================

        [HttpGet("packages")]
        public async Task<IActionResult> GetPackages()
        {
            var packages = await _context.TicketPackages
                                         .Where(p => p.IsActive)
                                         .ToListAsync();
            return Ok(packages);
        }

        [HttpGet("status/{deviceId}")]
        public async Task<IActionResult> CheckDeviceStatus(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return BadRequest("Mã thiết bị không hợp lệ");

            var activeTicket = await _context.TouristTickets
                .Where(t => t.DeviceId == deviceId && t.ExpiryDate > DateTime.Now)
                .OrderByDescending(t => t.ExpiryDate)
                .FirstOrDefaultAsync();

            if (activeTicket != null)
            {
                return Ok(new
                {
                    valid = true,
                    expiresAt = activeTicket.ExpiryDate,
                    remainingHours = (activeTicket.ExpiryDate - DateTime.Now).TotalHours
                });
            }

            return Ok(new { valid = false });
        }

        public class PurchaseRequest
        {
            public string DeviceId { get; set; } = string.Empty;
            public int PackageId { get; set; }
        }

        [HttpPost("purchase")]
        public async Task<IActionResult> PurchaseDeviceTicket([FromBody] PurchaseRequest request)
        {
            var package = await _context.TicketPackages.FindAsync(request.PackageId);
            if (package == null) return NotFound("Gói cước không tồn tại");

            // Tạo mã đơn hàng tạm bắt đầu bằng 9 để dễ phân biệt trong Webhook
            string tempId = "9" + new Random().Next(100, 999).ToString() + DateTime.Now.Second;
            
            // Lưu xuống ổ đĩa chờ thanh toán
            var pendingAppTicket = new PendingTicketDto 
            { 
                DeviceId = request.DeviceId, 
                PackageId = request.PackageId,
                Price = package.Price,
                DurationHours = package.DurationHours,
                PackageName = package.PackageName
            };
            
            var dict = System.IO.File.Exists(_pendingTicketsFile) ? JsonSerializer.Deserialize<Dictionary<string, PendingTicketDto>>(System.IO.File.ReadAllText(_pendingTicketsFile)) ?? new() : new();
            dict[tempId] = pendingAppTicket;
            System.IO.File.WriteAllText(_pendingTicketsFile, JsonSerializer.Serialize(dict));

            // Dùng proxy qua API của chúng ta để Giả lập Android tải được (Vì giả lập của bạn có vẻ bị ngắt kết nối DNS Internet ngoài vòng localhost)
            string vietQrBank = _bankName.ToLower().Contains("mb") ? "MB" : 
                                _bankName.ToLower().Contains("techcombank") ? "TCB" : _bankName;
                                
            string originalQrUrl = $"https://img.vietqr.io/image/{vietQrBank}-{_soTaiKhoan}-compact2.png?amount={(int)package.Price}&addInfo=DH{tempId}&accountName={_accountName}";
            
            // Encode the URL so it can be passed via Query String
            string encodedUrl = Uri.EscapeDataString(originalQrUrl);
            string qrUrl = $"/api/Tickets/proxy-qr?url={encodedUrl}";

            Console.WriteLine($"\n[FILE] Đã treo đơn hàng mua vé DH{tempId} cho Device: {request.DeviceId}\n");

            // Frontend sẽ dùng orderId để gọi check-status
            return Ok(new { success = true, qrUrl = qrUrl, orderId = tempId });
        }

        [HttpGet("proxy-qr")]
        public async Task<IActionResult> ProxyQr([FromQuery] string url)
        {
            try
            {
                var httpClient = new HttpClient();
                // Add a user-agent to avoid being blocked by some CDNs
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                
                var bytes = await httpClient.GetByteArrayAsync(url);
                return File(bytes, "image/png");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
    
    public class PendingTicketDto
    {
        public string DeviceId { get; set; } = "";
        public int PackageId { get; set; }
        public decimal Price { get; set; }
        public int DurationHours { get; set; }
        public string PackageName { get; set; } = "";
    }
}
