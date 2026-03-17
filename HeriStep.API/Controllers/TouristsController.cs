using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TouristsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public TouristsController(HeriStepDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách khách hàng đã mua vé
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TouristTicket>>> GetTourists()
        {
            var tourists = await (from t in _context.TouristTickets
                                  join p in _context.TicketPackages on t.PackageId equals p.Id
                                  orderby t.CreatedAt descending // Sắp xếp khách mới mua lên đầu
                                  select new TouristTicket
                                  {
                                      Id = t.Id,
                                      TicketCode = t.TicketCode,
                                      DeviceId = t.DeviceId,
                                      PackageId = t.PackageId,
                                      PackageName = p.PackageName, // Tên gói vé (VD: Vé 1 tuần)
                                      AmountPaid = t.AmountPaid,
                                      PaymentMethod = t.PaymentMethod,
                                      CreatedAt = t.CreatedAt,
                                      ExpiryDate = t.ExpiryDate
                                  }).ToListAsync();

            return Ok(tourists);
        }
    }
}