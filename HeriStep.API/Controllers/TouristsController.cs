using HeriStep.Shared.Models.DTOs.Requests;
using HeriStep.Shared.Models.DTOs.Responses;
using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HeriStep.API.Controllers { [Route("api/[controller]")][ApiController] public class TouristsController : ControllerBase { private readonly HeriStepDbContext _context; public TouristsController(HeriStepDbContext context) { _context = context; }          /*  L?y danh sách khách hàng dã mua vé */ [HttpGet] public async Task<ActionResult<IEnumerable<TouristTicket>>> GetTourists() { var tourists = await (from t in _context.TouristTickets join p in _context.TicketPackages on t.PackageId equals p.Id orderby t.CreatedAt descending /*  S?p x?p khách m?i mua lên d?u */ select new TouristTicket { Id = t.Id, TicketCode = t.TicketCode, DeviceId = t.DeviceId, PackageId = t.PackageId, PackageName = p.PackageName, /*  Tên gói vé (VD: Vé 1 tu?n) */ AmountPaid = t.AmountPaid, PaymentMethod = t.PaymentMethod, CreatedAt = t.CreatedAt, ExpiryDate = t.ExpiryDate }).ToListAsync(); return Ok(tourists); } } }

