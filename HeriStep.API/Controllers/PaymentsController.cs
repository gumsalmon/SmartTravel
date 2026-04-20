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

            string tempId = new Random().Next(100, 999).ToString() + DateTime.Now.Second;
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
            string qrUrl = $"https://qr.sepay.vn/img?bank={_bankName}&acc={_soTaiKhoan}&amount=2000&des=DH{stallId}";
            return Ok(new { qrUrl = qrUrl, orderId = stallId.ToString() });
        }

        // =======================================================
        // 5. MUA VÉ DU LỊCH TỪ APP KHÁCH (ĐA GÓI CƯỚC - ĐỒNG GIÁ 2K)
        // =======================================================
        [HttpPost("buy-tourist-ticket")]
        public IActionResult BuyTouristTicket([FromBody] PendingTicketDto req)
        {
            if (string.IsNullOrEmpty(req.DeviceId))
                return BadRequest(new { message = "Thiếu thông tin thiết bị (DeviceId)!" });

            // 🔥 FIX CỨNG: Gói nào cũng thu 2000đ để Test Demo Đồ án
            decimal giaTienGiaoDich = 2000;
            int thoiGianVe = 24;

            // Vẫn giữ Switch-Case để cấp đúng thời hạn (Duration) của vé cho khách
            switch (req.PackageId)
            {
                case 1:
                    thoiGianVe = 24; // Gói 1: Hạn 24 giờ
                    break;
                case 2:
                    thoiGianVe = 72; // Gói 2: Hạn 3 ngày
                    break;
                default:
                    thoiGianVe = 24; // Mặc định 24h
                    break;
            }

            req.Price = giaTienGiaoDich;
            req.DurationHours = thoiGianVe;

            string tempId = "9" + new Random().Next(100, 999).ToString() + DateTime.Now.Second;

            var dict = System.IO.File.Exists(_pendingTicketsFile)
                ? JsonSerializer.Deserialize<Dictionary<string, PendingTicketDto>>(System.IO.File.ReadAllText(_pendingTicketsFile)) ?? new()
                : new();
            dict[tempId] = req;
            System.IO.File.WriteAllText(_pendingTicketsFile, JsonSerializer.Serialize(dict));

            string qrUrl = $"https://qr.sepay.vn/img?bank={_bankName}&acc={_soTaiKhoan}&amount={giaTienGiaoDich}&des=DH{tempId}";
            Console.WriteLine($"\n[FILE] Đơn vé DH{tempId} | Thiết bị: {req.DeviceId} | Package: {req.PackageId} | Mức giá Test: {giaTienGiaoDich}\n");

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
                var match = Regex.Match(fullText, @"DH\s*(\d+)");

                if (match.Success)
                {
                    string idValue = match.Groups[1].Value;

                    // 🟢 TRƯỜNG HỢP 1: ĐĂNG KÝ MERCHANT MỚI
                    var pendingNew = ExtractPendingFromDisk(idValue);
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
                            Console.WriteLine($"[LỖI TẠO DB] {ex.Message}");
                        }
                        return Ok(new { success = true });
                    }

                    // 🟡 TRƯỜNG HỢP 2: MUA THÊM SẠP CHO MERCHANT HIỆN TẠI
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

                    // 🔵 TRƯỜNG HỢP 3: GIA HẠN SẠP CŨ 
                    if (int.TryParse(idValue, out int existingStallId))
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

                    // 🟣 TRƯỜNG HỢP 4: MUA VÉ DU LỊCH BẰNG APP CLIENT 
                    if (idValue.StartsWith("9"))
                    {
                        var pendingTicket = ExtractPendingTicketFromDisk(idValue);
                        if (pendingTicket != null)
                        {
                            // 💡 Vì giá đã bị ép cứng 2000đ lúc sinh đơn, nên chỗ này nó sẽ so sánh khách có chuyển đủ 2000đ hay không.
                            if (data.transferAmount < pendingTicket.Price)
                            {
                                Console.WriteLine($"[CẢNH BÁO] Đơn DH{idValue} chuyển THIẾU TIỀN! Yêu cầu: {pendingTicket.Price}, Thực tế: {data.transferAmount}");
                                var dict = System.IO.File.Exists(_pendingTicketsFile) ? JsonSerializer.Deserialize<Dictionary<string, PendingTicketDto>>(System.IO.File.ReadAllText(_pendingTicketsFile)) ?? new() : new();
                                dict[idValue] = pendingTicket;
                                System.IO.File.WriteAllText(_pendingTicketsFile, JsonSerializer.Serialize(dict));
                                return Ok(new { success = true });
                            }

                            Console.WriteLine($"=> [WEBHOOK] Tiền mua vé đã về! Kích hoạt vé cho thiết bị: {pendingTicket.DeviceId}");
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

                                Console.WriteLine($"🎉 [THÀNH CÔNG] Đã cấp Mã vé {ticketCode} cho App Client!");
                                _paidOrders.TryAdd(idValue, true);
                            }
                            catch (Exception ex)
                            {
                                await transaction.RollbackAsync();
                                var dict = System.IO.File.Exists(_pendingTicketsFile) ? JsonSerializer.Deserialize<Dictionary<string, PendingTicketDto>>(System.IO.File.ReadAllText(_pendingTicketsFile)) ?? new() : new();
                                dict[idValue] = pendingTicket;
                                System.IO.File.WriteAllText(_pendingTicketsFile, JsonSerializer.Serialize(dict));
                                Console.WriteLine($"[LỖI TẠO VÉ DB] {ex.Message}");
                            }
                            return Ok(new { success = true });
                        }
                    }
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[CRITICAL ERROR] {ex.Message} | {ex.InnerException?.Message}\n");
                return Ok(new { success = false });
            }
        }
    } // <-- Đã xóa cái ngoặc nhọn gây họa ở đây!

    // =======================================================
    // CÁC LỚP DTO 
    // =======================================================
    public class RegisterMerchantDto { public string FullName { get; set; } = ""; public string Phone { get; set; } = ""; public string Password { get; set; } = ""; public string StallName { get; set; } = ""; public double Latitude { get; set; } public double Longitude { get; set; } }
    public class AddStallDto { public int OwnerId { get; set; } public string StallName { get; set; } = ""; public double Latitude { get; set; } public double Longitude { get; set; } }
    public class SePayWebhookData { public string? transferType { get; set; } public decimal transferAmount { get; set; } public string? code { get; set; } public string? content { get; set; } }

 
}