using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// 1. ĐÃ FIX: Đăng ký HttpClient chuẩn chỉ
builder.Services.AddHttpClient();
builder.Services.AddScoped(sp => {
    var client = new HttpClient();
    client.BaseAddress = new Uri("http://127.0.0.1:5297/");
    return client;
});

// ==================================================
// 💡 ĐÃ FIX LỖI CRASH: Bắt buộc phải có dòng này trước khi AddSession
builder.Services.AddDistributedMemoryCache();
// ==================================================

// 2. Kích hoạt Session
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 3. Cấu hình Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.AccessDeniedPath = "/Login";
    });

builder.Services.AddAuthorization(); // Thêm dòng này cho chắc cú

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Chỉ gọi 1 lần duy nhất ở đây!
app.UseRouting();

// 4. Khởi chạy Session
app.UseSession();

// 5. Kích hoạt Xác thực danh tính 
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();