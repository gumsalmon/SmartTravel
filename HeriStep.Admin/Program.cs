using Microsoft.AspNetCore.Authentication.Cookies; // BƯỚC 1: Thêm thư viện này

var builder = WebApplication.CreateBuilder(args);

// 1. ĐĂNG KÝ HTTPCLIENT: Dòng này để Web Admin gọi được vào API
// Thay cổng 5297 bằng cổng thực tế của HeriStep.API nếu khác
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5297/")
});

// ==========================================
// 2. CẤU HÌNH BẢO MẬT & ĐĂNG NHẬP (THÊM MỚI Ở ĐÂY)
// ==========================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login"; // Nếu chưa đăng nhập, tự động đá về trang này
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // Cho phép login tối đa 8 tiếng
    });

// 3. KHÓA CỬA TOÀN BỘ TRANG WEB (SỬA LẠI ĐOẠN AddRazorPages CŨ)
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/"); // Bắt buộc đăng nhập cho mọi trang (/, /Index, /Stalls...)
    options.Conventions.AllowAnonymousToPage("/Login"); // NGOẠI TRỪ trang Login thì ai cũng được vào
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Cho phép dùng CSS/JS/Ảnh trong wwwroot

app.UseRouting();

// ==========================================
// 4. KÍCH HOẠT CHẾ ĐỘ BẢO VỆ (THÊM UseAuthentication VÀO ĐÂY)
// Lưu ý: Authentication phải đứng TRƯỚC Authorization
// ==========================================
app.UseAuthentication(); // <-- Dòng này để hỏi "Bạn là ai? Có thẻ/cookie chưa?"
app.UseAuthorization();  // <-- Dòng này để hỏi "Bạn có quyền vào trang này không?"

app.MapRazorPages();

app.Run();