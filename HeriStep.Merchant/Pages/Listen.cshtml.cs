using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HeriStep.Merchant.Pages
{
    public class ListenModel : PageModel
    {
        public int StallId { get; set; }

        public void OnGet(int stallId)
        {
            StallId = stallId;
        }
    }
}
