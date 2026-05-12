using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Collections.Concurrent;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;
        private readonly string _bankName;
        private readonly string _soTaiKhoan;
        private readonly string _pendingFile = "pending_stalls.json";
        private readonly string _extraStallFile = "pending_extra_stalls.json";
        private readonly string _pendingTicketsFile = "pending_tickets.json";

        private static readonly ConcurrentDictionary<string, bool> _paidOrders = new();

        public PaymentsController(HeriStepDbContext context, IConfiguration config)
        {
            _context = context;
            _bankName = config["SePay:BankName"] ?? "MBBank";
            _soTaiKhoan = config["SePay:BankAccount"] ?? "0388764276";
        }

        // =======================================================
        // HÀM HỖ TRỢ ĐỌC GHI FILE TẠM
        // =======================================================
        private void SavePendingToDisk(string id, RegisterMerchantDto req)
        {
            var dict = System.IO.File.Exists(_pendingFile) ? JsonSerializer.Deserialize<Dictionary<string, RegisterMerchantDto>>(System.IO.File.ReadAllText(_pendingFile)) ?? new() : new();
            dict[id] = req;
            System.IO.File.WriteAllText(_pendingFile, JsonSerializer.Serialize(dict));
        }

        private RegisterMerchantDto? ExtractPendingFromDisk(string id)
        {
            if (!System.IO.File.Exists(_pendingFile)) return null;
            var dict = JsonSerializer.Deserialize<Dictionary<string, RegisterMerchantDto>>(System.IO.File.ReadAllText(_pendingFile)) ?? new();
            if (dict.TryGetValue(id, out var req))
            {
                dict.Remove(id);
                System.IO.File.WriteAllText(_pendingFile, JsonSerializer.Serialize(dict));
                return req;
            }
            return null;
        }

        private void SaveExtraStallToDisk(string id, AddStallDto req)
        {
            var dict = System.IO.File.Exists(_extraStallFile) ? JsonSerializer.Deserialize<Dictionary<string, AddStallDto>>(System.IO.File.ReadAllText(_extraStallFile)) ?? new() : new();
            dict[id] = req;
            System.IO.File.WriteAllText(_extraStallFile, JsonSerializer.Serialize(dict));
        }

        private AddStallDto? ExtractExtraStallFromDisk(string id)
        {
            if (!System.IO.File.Exists(_extraStallFile)) return null;
            var dict = JsonSerializer.Deserialize<Dictionary<string, AddStallDto>>(System.IO.File.ReadAllText(_extraStallFile)) ?? new();
            if (dict.TryGetValue(id, out var req))
            {
                dict.Remove(id);
                System.IO.File.WriteAllText(_extraStallFile, JsonSerializer.Serialize(dict));
                return req;
            }
            return null;
        }

        private PendingTicketDto? ExtractPendingTicketFromDisk(string id)
        {
            if (!System.IO.File.Exists(_pendingTicketsFile)) return null;
            var dict = JsonSerializer.Deserialize<Dictionary<string, PendingTicketDto>>(System.IO.File.ReadAllText(_pendingTicketsFile)) ?? new();
            if (dict.TryGetValue(id, out var req))
            {
                dict.Remove(id);
                System.IO.File.WriteAllText(_pendingTicketsFile, JsonSerializer.Serialize(dict));
                return req;
            }
            return null;
        }

        // =======================================================
        // API CHECK TRẠNG THÁI 
        // =======================================================
        [HttpGet("check-status/{orderId}")]
        public IActionResult CheckStatus(string orderId)
        {
            bool isPaid = _paidOrders.ContainsKey(orderId);
            if (isPaid)
            {
                _paidOrders.TryRemove(orderId, out _);
            }
            return Ok(new { isPaid });
        }

        // =======================================================
        // 1. ĐĂNG KÝ MERCHANT MỚI 
        // =======================================================
        [HttpPost("register-new-merchant")]
        public async Task<IActionResult> RegisterNewMerchant([FromBody] RegisterMerchantDto req)
        {
            if (await _context.Users.AnyAsync(u => u.Username == req.Phone))
                return BadRequest(new { message = "Số điện thoại đã tồn tại tài khoản thật!" });

            // Tiền tố '1' dành cho Merchant mới
            string tempId = "1" + new Random().Next(100, 999).ToString() + DateTime.Now.Second;
            SavePendingToDisk(tempId, req);

            string qrUrl = $"https://qr.sepay.vn/img?bank={_bankName}&acc={_soTaiKhoan}&amount=2000&des=DH{tempId}";
            Console.WriteLine($"\n[FILE] Đã treo đơn hàng tạm DH{tempId} cho SĐT: {req.Phone}\n");

            return Ok(new { qrUrl = qrUrl, orderId = tempId });
        }

        // =======================================================
        // 2. MUA THÊM SẠP
        // =======================================================
        [HttpPost("buy-extra-stall")]
        public async Task<IActionResult> BuyExtraStall([FromBody] AddStallDto req)
        {
            if (!await _context.Users.AnyAsync(u => u.Id == req.OwnerId && !u.IsDeleted))
                return BadRequest(new { message = "Không tìm thấy tài khoản Chủ sạp!" });

            // Tiền tố '8' dành cho mua thêm sạp
            string tempId = "8" + new Random().Next(100, 999).ToString() + DateTime.Now.Second;
            SaveExtraStallToDisk(tempId, req);

            string qrUrl = $"https://qr.sepay.vn/img?bank={_bankName}&acc={_soTaiKhoan}&amount=2000&des=DH{tempId}";
            Console.WriteLine($"\n[FILE] Đã treo đơn hàng mua sạp phụ DH{tempId} cho Chủ sạp ID: {req.OwnerId}\n");

            return Ok(new { qrUrl = qrUrl, orderId = tempId });
        }

        // =======================================================
        // 3. LẤY QR GIA HẠN SẠP CŨ
        // =======================================================
        [HttpGet("get-renewal-qr/{stallId}")]
        public IActionResult GetRenewalQr(int stallId)
        {
            // Tiền tố '2' dành cho Gia hạn sạp cũ
            string orderId = "2" + stallId.ToString();
            string qrUrl = $"https://qr.sepay.vn/img?bank={_bankName}&acc={_soTaiKhoan}&amount=2000&des=DH{orderId}";
            return Ok(new { qrUrl = qrUrl, orderId = orderId });
        }

        // =======================================================
        // 5. MUA VÉ DU LỊCH TỪ APP KHÁCH (ĐA GÓI CƯỚC - ĐỒNG GIÁ 2K)
        // =======================================================
        [HttpPost("buy-tourist-ticket")]
        public IActionResult BuyTouristTicket([FromBody] PendingTicketDto req)
        {
            if (string.IsNullOrEmpty(req.DeviceId))
                return BadRequest(new { message = "Thiếu thông tin thiết bị (DeviceId)!" });

            decimal giaTienGiaoDich = 2000;
            int thoiGianVe = 24;

            switch (req.PackageId)
            {
                case 1: thoiGianVe = 24; break;
                case 2: thoiGianVe = 72; break;
                default: thoiGianVe = 24; break;
            }

            req.Price = giaTienGiaoDich;
            req.DurationHours = thoiGianVe;

            // Tiền tố '9' dành cho vé du lịch
            string tempId = "9" + new Random().Next(100, 999).ToString() + DateTime.Now.Second;

            var dict = System.IO.File.Exists(_pendingTicketsFile)
                ? JsonSerializer.Deserialize<Dictionary<string, PendingTicketDto>>(System.IO.File.ReadAllText(_pendingTicketsFile)) ?? new()
                : new();
            dict[tempId] = req;
            System.IO.File.WriteAllText(_pendingTicketsFile, JsonSerializer.Serialize(dict));

            string qrUrl = $"https://qr.sepay.vn/img?bank={_bankName}&acc={_soTaiKhoan}&amount={giaTienGiaoDich}&des=DH{tempId}";
            Console.WriteLine($"\n[FILE] Đơn vé DH{tempId} | Thiết bị: {req.DeviceId} | Package: {req.PackageId}\n");

            return Ok(new { qrUrl = qrUrl, orderId = tempId });
        }

        // =======================================================
        // 4. WEBHOOK XỬ LÝ THANH TOÁN
        // =======================================================
        [HttpPost("sepay-webhook")]
        public async Task<IActionResult> SePayWebhook([FromBody] SePayWebhookData data)
        {
            try
            {
                if (data == null || data.transferType?.ToLower() != "in")
                    return Ok(new { success = true });

                string fullText = $"{data.code} {data.content}".ToUpper();
                System.IO.File.AppendAllText("webhook_log.txt", $"[{DateTime.Now}] Received: {fullText} | Amt: {data.transferAmount}\n");
                
                var match = Regex.Match(fullText, @"DH\s*(\d+)");

                if (match.Success)
                {
                    string idValue = match.Groups[1].Value;
                    System.IO.File.AppendAllText("webhook_log.txt", $"[{DateTime.Now}] ID match: {idValue}\n");

                    // 🟢 TRƯỜNG HỢP 1: ĐĂNG KÝ MERCHANT MỚI (Tiền tố '1' hoặc mã cũ 5 số)
                    if (idValue.StartsWith("1") || (idValue.Length == 5 && !idValue.StartsWith("9") && !idValue.StartsWith("8")))
                    {
                        var pendingNew = ExtractPendingFromDisk(idValue);
                        if (pendingNew == null) {
                            System.IO.File.AppendAllText("webhook_log.txt", $"[{DateTime.Now}] NOT FOUND in pending_stalls.json: {idValue}\n");
                        } else {
                            System.IO.File.AppendAllText("webhook_log.txt", $"[{DateTime.Now}] FOUND in pending_stalls.json: {idValue} | Phone: {pendingNew.Phone}\n");
                        }

                        if (pendingNew != null)
                        {
                            if (data.transferAmount < 2000) return Ok(new { success = true });

                            Console.WriteLine($"=> [WEBHOOK] Tiền về! Đang tạo tài khoản cho SĐT: {pendingNew.Phone}");
                            using var transaction = await _context.Database.BeginTransactionAsync();
                            try
                            {
                                var newUser = new User
                                {
                                    Username = pendingNew.Phone,
                                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(pendingNew.Password),
                                    FullName = pendingNew.FullName,
                                    Role = "StallOwner"
                                };
                                _context.Users.Add(newUser);
                                await _context.SaveChangesAsync();

                                var newStall = new Stall
                                {
                                    OwnerId = newUser.Id,
                                    Name = pendingNew.StallName,
                                    Latitude = pendingNew.Latitude,
                                    Longitude = pendingNew.Longitude,
                                    IsOpen = true,
                                    SortOrder = 0
                                };
                                _context.Stalls.Add(newStall);
                                await _context.SaveChangesAsync();

                                _context.Subscriptions.Add(new Subscription
                                {
                                    StallId = newStall.Id,
                                    DeviceId = "DEV-" + newStall.Id,
                                    IsActive = true,
                                    StartDate = DateTime.Now,
                                    ExpiryDate = DateTime.Now.AddDays(30),
                                    ActivationCode = "ACT-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
                                });

                                _context.SubscriptionTransactions.Add(new SubscriptionTransaction
                                {
                                    StallId = newStall.Id,
                                    Amount = data.transferAmount,
                                    Note = "Đăng ký Merchant mới"
                                });

                                await _context.SaveChangesAsync();
                                await transaction.CommitAsync();
                                Console.WriteLine($"🎉 [THÀNH CÔNG] Đã tạo User {newUser.Id} và Sạp {newStall.Id}!");

                                _paidOrders.TryAdd(idValue, true);
                            }
                            catch (Exception ex)
                            {
                                await transaction.RollbackAsync();
                                SavePendingToDisk(idValue, pendingNew);
                                System.IO.File.AppendAllText("webhook_log.txt", $"[{DateTime.Now}] DB ERROR (Case 1): {ex.Message} | {ex.InnerException?.Message}\n");
                                Console.WriteLine($"[LỖI TẠO DB] {ex.Message}");
                            }
                            return Ok(new { success = true });
                        }
                    }

                    // 🟡 TRƯỜNG HỢP 2: MUA THÊM SẠP (Tiền tố '8')
                    if (idValue.StartsWith("8"))
                    {
                        var pendingExtra = ExtractExtraStallFromDisk(idValue);
                        if (pendingExtra != null)
                        {
                            if (data.transferAmount < 2000) return Ok(new { success = true });

                            Console.WriteLine($"=> [WEBHOOK] Tiền về! Đang tạo thêm sạp cho Chủ sạp ID: {pendingExtra.OwnerId}");
                            using var transaction = await _context.Database.BeginTransactionAsync();
                            try
                            {
                                var extraStall = new Stall
                                {
                                    OwnerId = pendingExtra.OwnerId,
                                    Name = pendingExtra.StallName,
                                    Latitude = pendingExtra.Latitude,
                                    Longitude = pendingExtra.Longitude,
                                    IsOpen = true,
                                    SortOrder = 0
                                };
                                _context.Stalls.Add(extraStall);
                                await _context.SaveChangesAsync();

                                _context.Subscriptions.Add(new Subscription
                                {
                                    StallId = extraStall.Id,
                                    DeviceId = "DEV-" + extraStall.Id,
                                    IsActive = true,
                                    StartDate = DateTime.Now,
                                    ExpiryDate = DateTime.Now.AddDays(30),
                                    ActivationCode = "ACT-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
                                });

                                _context.SubscriptionTransactions.Add(new SubscriptionTransaction
                                {
                                    StallId = extraStall.Id,
                                    Amount = data.transferAmount,
                                    Note = "Mua thêm sạp phụ"
                                });

                                await _context.SaveChangesAsync();
                                await transaction.CommitAsync();
                                Console.WriteLine($"🎉 [THÀNH CÔNG] Đã tạo thêm Sạp phụ {extraStall.Id} cho User {pendingExtra.OwnerId}!");

                                _paidOrders.TryAdd(idValue, true);
                            }
                            catch (Exception ex)
                            {
                                await transaction.RollbackAsync();
                                SaveExtraStallToDisk(idValue, pendingExtra);
                                Console.WriteLine($"[LỖI TẠO DB] {ex.Message}");
                            }
                            return Ok(new { success = true });
                        }
                    }

                    // 🔵 TRƯỜNG HỢP 3: GIA HẠN SẠP CŨ (Tiền tố '2')
                    if (idValue.StartsWith("2"))
                    {
                        string realStallIdStr = idValue.Substring(1);
                        if (int.TryParse(realStallIdStr, out int existingStallId))
                        {
                            if (data.transferAmount < 2000) return Ok(new { success = true });

                            var stall = await _context.Stalls.FindAsync(existingStallId);
                            if (stall != null)
                            {
                                stall.IsOpen = true;
                                var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.StallId == existingStallId);
                                if (sub != null)
                                {
                                    sub.IsActive = true;
                                    sub.ExpiryDate = (!sub.ExpiryDate.HasValue || sub.ExpiryDate < DateTime.Now) ? DateTime.Now.AddDays(30) : sub.ExpiryDate.Value.AddDays(30);
                                }
                                _context.SubscriptionTransactions.Add(new SubscriptionTransaction { StallId = existingStallId, Amount = data.transferAmount, Note = "Gia hạn sạp" });

                                await _context.SaveChangesAsync();
                                Console.WriteLine($"🎉 [THÀNH CÔNG] Đã gia hạn sạp {existingStallId} thành công!");

                                _paidOrders.TryAdd(idValue, true);
                                return Ok(new { success = true });
                            }
                        }
                    }

                    // 🟣 TRƯỜNG HỢP 4: MUA VÉ DU LỊCH (Tiền tố '9')
                    if (idValue.StartsWith("9"))
                    {
                        var pendingTicket = ExtractPendingTicketFromDisk(idValue);
                        if (pendingTicket != null)
                        {
                            if (data.transferAmount < pendingTicket.Price)
                            {
                                Console.WriteLine($"[CẢNH BÁO] Đơn DH{idValue} thiếu tiền!");
                                return Ok(new { success = true });
                            }

                            Console.WriteLine($"=> [WEBHOOK] Kích hoạt vé cho thiết bị: {pendingTicket.DeviceId}");
                            using var transaction = await _context.Database.BeginTransactionAsync();
                            try
                            {
                                var ticketCode = $"DEV-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                                var newTicket = new TouristTicket
                                {
                                    DeviceId = pendingTicket.DeviceId,
                                    PackageId = pendingTicket.PackageId,
                                    AmountPaid = pendingTicket.Price,
                                    TicketCode = ticketCode,
                                    PaymentMethod = "SePay_" + data.transferAmount,
                                    CreatedAt = DateTime.Now,
                                    ExpiryDate = DateTime.Now.AddHours(pendingTicket.DurationHours)
                                };
                                _context.TouristTickets.Add(newTicket);
                                await _context.SaveChangesAsync();
                                await transaction.CommitAsync();

                                Console.WriteLine($"🎉 [THÀNH CÔNG] Đã cấp Mã vé {ticketCode}!");
                                _paidOrders.TryAdd(idValue, true);
                            }
                            catch (Exception ex)
                            {
                                await transaction.RollbackAsync();
                                Console.WriteLine($"[LỖI TẠO VÉ] {ex.Message}");
                            }
                            return Ok(new { success = true });
                        }
                    }
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[CRITICAL ERROR] {ex.Message}\n");
                return Ok(new { success = false });
            }
        }
    }

    public class RegisterMerchantDto { public string FullName { get; set; } = ""; public string Phone { get; set; } = ""; public string Password { get; set; } = ""; public string StallName { get; set; } = ""; public double Latitude { get; set; } public double Longitude { get; set; } }
    public class AddStallDto { public int OwnerId { get; set; } public string StallName { get; set; } = ""; public double Latitude { get; set; } public double Longitude { get; set; } }
    public class SePayWebhookData { public string? transferType { get; set; } public decimal transferAmount { get; set; } public string? code { get; set; } public string? content { get; set; } }
}