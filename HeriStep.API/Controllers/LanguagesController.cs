using HeriStep.Shared.Models.DTOs.Requests;
using HeriStep.Shared.Models.DTOs.Responses;
using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HeriStep.API.Controllers { [Route("api/[controller]")][ApiController] public class LanguagesController : ControllerBase { private readonly HeriStepDbContext _context; public LanguagesController(HeriStepDbContext context) { _context = context; }          /*  1. L?y danh sách t?t c? ngôn ng? dang h? tr? */ [HttpGet] public async Task<ActionResult<IEnumerable<Language>>> GetLanguages() { return await _context.Languages.ToListAsync(); }          /*  2. Thêm m?t ngôn ng? m?i (VD: Thêm 'th' - Ti?ng Thái) */ [HttpPost] public async Task<ActionResult<Language>> AddLanguage(Language language) { if (await _context.Languages.AnyAsync(l => l.LangCode == language.LangCode)) { return BadRequest("Mã ngôn ng? này dã t?n t?i!"); } _context.Languages.Add(language); await _context.SaveChangesAsync(); return Ok(language); }          /*  3. Xóa m?t ngôn ng? */ [HttpDelete("{langCode}")] public async Task<IActionResult> DeleteLanguage(string langCode) { var lang = await _context.Languages.FindAsync(langCode); if (lang == null) return NotFound(); _context.Languages.Remove(lang); await _context.SaveChangesAsync(); return Ok(new { Message = "Ðã xóa ngôn ng? thành công!" }); } } }

