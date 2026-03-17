using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LanguagesController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public LanguagesController(HeriStepDbContext context)
        {
            _context = context;
        }

        // 1. Lấy danh sách tất cả ngôn ngữ đang hỗ trợ
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Language>>> GetLanguages()
        {
            return await _context.Languages.ToListAsync();
        }

        // 2. Thêm một ngôn ngữ mới (VD: Thêm 'th' - Tiếng Thái)
        [HttpPost]
        public async Task<ActionResult<Language>> AddLanguage(Language language)
        {
            if (await _context.Languages.AnyAsync(l => l.LangCode == language.LangCode))
            {
                return BadRequest("Mã ngôn ngữ này đã tồn tại!");
            }

            _context.Languages.Add(language);
            await _context.SaveChangesAsync();

            return Ok(language);
        }

        // 3. Xóa một ngôn ngữ
        [HttpDelete("{langCode}")]
        public async Task<IActionResult> DeleteLanguage(string langCode)
        {
            var lang = await _context.Languages.FindAsync(langCode);
            if (lang == null) return NotFound();

            _context.Languages.Remove(lang);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đã xóa ngôn ngữ thành công!" });
        }
    }
}