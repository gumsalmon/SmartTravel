using HeriStep.API.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. CÁCH ĐĂNG KÝ HTTPCLIENT CHUẨN SERVER-SIDE
builder.Services.AddHttpClient();
builder.Services.AddScoped(sp => {
    var client = new HttpClient();
    client.BaseAddress = new Uri("http://localhost:5297/"); // Đảm bảo API đang chạy ở cổng này
    return client;
});
// 1. ĐĂNG KÝ DATABASE (Cho Analytics & Dashboard)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<HeriStepDbContext>(options =>
    options.UseSqlServer(connectionString));
// 2. CẤU HÌNH BẢO MẬT & ĐĂNG NHẬP
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// Bổ sung dòng này để hệ thống hiểu các quy tắc phân quyền
builder.Services.AddAuthorization();

// 3. KHÓA CỬA TOÀN BỘ TRANG WEB
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
});

var app = builder.Build();

// ==============================================================
// 💡 ĐÃ FIX: TỰ ĐỘNG GỌI API KHỞI TẠO 10 TOUR KHI VỪA BẬT WEB
// ==============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 4. KÍCH HOẠT CHẾ ĐỘ BẢO VỆ
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();