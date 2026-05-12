using HeriStep.Admin.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Yarp.ReverseProxy.Configuration;


var builder = WebApplication.CreateBuilder(args);

// ================================================================
// 1. HTTP CLIENT (gọi Backend API)
// ================================================================
builder.Services.AddHttpClient();
builder.Services.AddSingleton(System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All));
builder.Services.AddScoped(sp =>
{
    var client = new HttpClient
    {
        BaseAddress = new Uri("http://127.0.0.1:5297/") // Port của HeriStep.API
    };
    return client;
});

// 💡 Cấu hình Proxy để chuyển hướng /api sang API Project
builder.Services.AddReverseProxy()
    .LoadFromMemory(
        routes: new[]
        {
            new RouteConfig
            {
                RouteId = "api-route",
                ClusterId = "api-cluster",
                Match = new RouteMatch { Path = "/api/{**remainder}" }
            }
        },
        clusters: new[]
        {
            new ClusterConfig
            {
                ClusterId = "api-cluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "api-destination", new DestinationConfig { Address = "http://127.0.0.1:5297/" } }
                }
            }
        });


// ================================================================
// 2. AUTHENTICATION (Cookie-based cho Admin Web)
// ================================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath          = "/Login";
        options.LogoutPath         = "/Logout";
        options.AccessDeniedPath   = "/Login";
        options.ExpireTimeSpan     = TimeSpan.FromHours(8);
        options.SlidingExpiration  = true;
    });

builder.Services.AddAuthorization();

// ================================================================
// 3. RAZOR PAGES (khóa toàn bộ, chỉ cho Login vào ẩn danh)
// ================================================================
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
});

// ================================================================
// 4. SIGNALR — ĐÂY LÀ PHẦN BỊ THIẾU GÂY RA BUG SỐ 0!
//    - Thêm SignalR service
//    - Cho phép fallback LongPolling vì Flutter SDK không dùng WebSocket native
// ================================================================
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval    = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
});

// ================================================================
// 5. CORS — Cho phép Flutter App và web client khác kết nối Hub
//    - WithCredentials = true yêu cầu khai báo origin tường minh
//    - Khai báo đầy đủ cả localhost dev và production domain
// ================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5297",   // Backend API (dev)
                "http://localhost:5287",   // Admin Web (dev self)
                "http://localhost:4200",   // Nếu có web client dev khác
                "https://vinhkhanh.click", // Production web
                "https://admin.vinhkhanh.click"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // BẮT BUỘC khi dùng WithCredentials từ client
    });
});

// ================================================================
var app = builder.Build();
// ================================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// CORS phải đứng TRƯỚC Authentication và Hub mapping
app.UseCors("SignalRPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapReverseProxy();


// ================================================================
// 6. MAP SIGNALR HUB — PHẦN BỊ THIẾU THỨ 2!
//    URL này Admin Web và Flutter App sẽ kết nối vào
// ================================================================
app.MapHub<DashboardHub>("/dashboardHub").RequireCors("SignalRPolicy");

app.Run();