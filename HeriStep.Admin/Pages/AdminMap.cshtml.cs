using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HeriStep.Admin.Pages
{
    public class AdminMapModel : PageModel
    {
        public void OnGet()
        {
            // Trang này chỉ cần load giao diện, data lấy bằng Javascript (Fetch API)
        }
    }
}