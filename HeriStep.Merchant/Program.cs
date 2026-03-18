using Microsoft.AspNetCore.Authentication.Cookies; // 💡 ĐÃ THÊM: Thư viện quản lý Cookie Đăng nhập

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// 1. Cấu hình HttpClient để gọi API
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5297/") });

// 2. Kích hoạt Session
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 💡 ĐÃ THÊM: 3. Cấu hình Cookie Authentication để sửa lỗi "No authenticationScheme"
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login"; // Chuyển hướng về trang Login nếu chưa đăng nhập
        options.ExpireTimeSpan = TimeSpan.FromDays(30); // Giữ đăng nhập 30 ngày cho Cô Ba đỡ cực
        options.AccessDeniedPath = "/Login"; // Không có quyền cũng đuổi về Login
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 4. Phải có dòng UseSession (nếu bạn có xài Session)
app.UseSession();

// 💡 ĐÃ THÊM: 5. Kích hoạt Xác thực danh tính (BẮT BUỘC NẰM TRƯỚC UseAuthorization)
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

app.MapRazorPages();
app.Run();