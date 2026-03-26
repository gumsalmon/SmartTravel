using HeriStep.Shared.Models.DTOs.Requests;
using HeriStep.Shared.Models.DTOs.Responses;
using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
[Route("api/[controller]")][ApiController] public class SubscriptionsController : ControllerBase { private readonly HeriStepDbContext _context; public SubscriptionsController(HeriStepDbContext context) => _context = context;      /*  1. L?Y DANH SÁCH GÓI CU?C THI?T B? */ [HttpGet] public async Task<ActionResult<IEnumerable<Subscription>>> GetSubscriptions() {         /*  Vì DB hi?n t?i chua có stall_id trong b?ng Subscriptions, */ /*  nên chúng ta s? l?y danh sách thu?n t? b?ng Subscriptions. */ return await _context.Subscriptions.ToListAsync(); }      /*  2. KÍCH HO?T THI?T B? M?I (Dùng cho Admin) */ [HttpPost] public async Task<ActionResult<Subscription>> PostSubscription(Subscription sub) {         /*  Ki?m tra xem DeviceId ho?c ActivationCode dã t?n t?i chua */ if (await _context.Subscriptions.AnyAsync(s => s.ActivationCode == sub.ActivationCode)) { return BadRequest("? Mã kích ho?t này dã du?c s? d?ng!"); }          /*  T? d?ng tính toán ngày */ sub.StartDate = DateTime.Now; sub.ExpiryDate = DateTime.Now.AddDays(30); /*  M?c d?nh 30 ngày */ sub.IsActive = true; _context.Subscriptions.Add(sub); await _context.SaveChangesAsync(); return Ok(sub); } }

