using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;
        private readonly string _soTaiKhoanMB;
        private readonly string _pendingFile = "pending_stalls.json"; // 💡 Ổ CỨNG LƯU TRỮ TẠM THỜI

        public PaymentsController(HeriStepDbContext context, IConfiguration config)
        {
            _context = context;
            _soTaiKhoanMB = config["SePay:BankAccount"] ?? "0366994409";
        }

        // =======================================================
        // HÀM HỖ TRỢ: ĐỌC GHI FILE TẠM (CHỐNG MẤT DỮ LIỆU)
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

        // =======================================================
        // 1. ĐĂNG KÝ MỚI
        // =======================================================
        [HttpPost("register-new-merchant")]
        public async Task<IActionResult> RegisterNewMerchant([FromBody] RegisterMerchantDto req)
        {
            if (await _context.Users.AnyAsync(u => u.Username == req.Phone))
                return BadRequest(new { message = "Số điện thoại đã tồn tại tài khoản thật!" });

            // Random ID
            string tempId = new Random().Next(100, 999).ToString() + DateTime.Now.Second;

            // Lưu vào ổ cứng (Không sợ mất khi restart app)
            SavePendingToDisk(tempId, req);

            string qrUrl = $"https://qr.sepay.vn/img?bank=MBBank&acc={_soTaiKhoanMB}&amount=2000&des=DH{tempId}";
            Console.WriteLine($"\n[FILE] Đã treo đơn hàng tạm DH{tempId} cho SĐT: {req.Phone}\n");

            return Ok(new { qrUrl = qrUrl });
        }

        // =======================================================
        // 2. LẤY QR GIA HẠN 
        // =======================================================
        [HttpGet("get-renewal-qr/{stallId}")]
        public IActionResult GetRenewalQr(int stallId)
        {
            string qrUrl = $"https://qr.sepay.vn/img?bank=MBBank&acc={_soTaiKhoanMB}&amount=2000&des=DH{stallId}";
            return Ok(new { qrUrl = qrUrl });
        }

        // =======================================================
        // 3. WEBHOOK XỬ LÝ THANH TOÁN (TRÁI TIM HỆ THỐNG)
        // =======================================================
        [HttpPost("sepay-webhook")]
        public async Task<IActionResult> SePayWebhook([FromBody] SePayWebhookData data)
        {
            try
            {
                // Chỉ xử lý nếu là tiền vào và đủ 2000đ
                if (data == null || data.transferType?.ToLower() != "in" || data.transferAmount < 2000)
                    return Ok(new { success = true });

                string fullText = $"{data.code} {data.content}".ToUpper();
                var match = Regex.Match(fullText, @"DH\s*(\d+)");

                if (match.Success)
                {
                    string idValue = match.Groups[1].Value;

                    // 🟢 TRƯỜNG HỢP 1: ĐĂNG KÝ MỚI (Móc từ File tạm ra)
                    var pending = ExtractPendingFromDisk(idValue);
                    if (pending != null)
                    {
                        Console.WriteLine($"=> [WEBHOOK] Tiền về! Đang tạo tài khoản cho SĐT: {pending.Phone}");

                        using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            var newUser = new User
                            {
                                Username = pending.Phone,
                                PasswordHash = BCrypt.Net.BCrypt.HashPassword(pending.Password),
                                FullName = pending.FullName,
                                Role = "StallOwner"
                            };
                            _context.Users.Add(newUser);
                            await _context.SaveChangesAsync();

                            var newStall = new Stall
                            {
                                OwnerId = newUser.Id,
                                Name = pending.StallName,
                                Latitude = pending.Latitude,
                                Longitude = pending.Longitude,
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
                                Note = "Đăng ký sạp mới"
                            });

                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();
                            Console.WriteLine($"🎉 [THÀNH CÔNG] Đã tạo User {newUser.Id} và Sạp {newStall.Id}!");
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            SavePendingToDisk(idValue, pending); // Bị lỗi thì cất lại vào file để tí thử lại
                            Console.WriteLine($"[LỖI TẠO DB] {ex.Message} | {ex.InnerException?.Message}");
                        }
                    }
                    // 🔵 TRƯỜNG HỢP 2: GIA HẠN CŨ (Sạp đã có sẵn trong SQL)
                    else if (int.TryParse(idValue, out int stallId))
                    {
                        var stall = await _context.Stalls.FindAsync(stallId);
                        if (stall != null)
                        {
                            stall.IsOpen = true;
                            var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.StallId == stallId);
                            if (sub != null)
                            {
                                sub.IsActive = true;
                                sub.ExpiryDate = (!sub.ExpiryDate.HasValue || sub.ExpiryDate < DateTime.Now) ? DateTime.Now.AddDays(30) : sub.ExpiryDate.Value.AddDays(30);
                            }
                            _context.SubscriptionTransactions.Add(new SubscriptionTransaction { StallId = stallId, Amount = data.transferAmount, Note = "Gia hạn sạp" });
                            await _context.SaveChangesAsync();
                            Console.WriteLine($"🎉 [THÀNH CÔNG] Đã gia hạn sạp {stallId} thành công!");
                        }
                        else
                        {
                            Console.WriteLine($"[CẢNH BÁO] Không tìm thấy mã DH{idValue} ở cả Đăng ký mới lẫn Gia hạn.");
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
    }

    public class RegisterMerchantDto { public string FullName { get; set; } = ""; public string Phone { get; set; } = ""; public string Password { get; set; } = ""; public string StallName { get; set; } = ""; public double Latitude { get; set; } public double Longitude { get; set; } }
    public class AddStallDto { public int OwnerId { get; set; } public string StallName { get; set; } = ""; public double Latitude { get; set; } public double Longitude { get; set; } }
    public class SePayWebhookData { public string? transferType { get; set; } public decimal transferAmount { get; set; } public string? code { get; set; } public string? content { get; set; } }
}