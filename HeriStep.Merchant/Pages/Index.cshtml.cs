using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HeriStep.Shared.Models;

namespace HeriStep.Merchant.Pages;

public class IndexModel : PageModel
{
    private readonly HttpClient _http;
    public IndexModel(HttpClient http) => _http = http;

    public PointOfInterest MyStall { get; set; } = new();
    public List<Product> MyProducts { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        // BƯỚC QUAN TRỌNG: Kiểm tra Session
        var stallId = HttpContext.Session.GetInt32("MyStallID");
        if (stallId == null) return RedirectToPage("Login");

        // Lấy dữ liệu hiển thị
        MyStall = await _http.GetFromJsonAsync<PointOfInterest>($"api/Points/{stallId}") ?? new();
        MyProducts = await _http.GetFromJsonAsync<List<Product>>($"api/Stalls/{stallId}/products") ?? new();
        return Page();
    }

    // Xử lý thêm món ăn mới
    public async Task<IActionResult> OnPostAddProductAsync(string pName, decimal pPrice, IFormFile pImage)
    {
        var stallId = HttpContext.Session.GetInt32("MyStallID");

        // 1. Upload ảnh lên API
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(pImage.OpenReadStream()), "file", pImage.FileName);
        var uploadRes = await _http.PostAsync("api/Points/upload", content);

        if (uploadRes.IsSuccessStatusCode)
        {
            // 2. Lưu thông tin món ăn vào SQL qua API
            var newProduct = new Product
            {
                StallId = stallId ?? 0,
                BasePrice = pPrice,
                ProductName = pName,
                ImageUrl = pImage.FileName
            };
            await _http.PostAsJsonAsync("api/Products", newProduct);
        }
        return RedirectToPage();
    }
}