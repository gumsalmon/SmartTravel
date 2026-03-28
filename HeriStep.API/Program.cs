using Microsoft.EntityFrameworkCore;
using HeriStep.API.Data;
using HeriStep.API.Services;
using HeriStep.API.Hubs;
using HeriStep.API.Interfaces;
using HeriStep.API.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- 1. ĐĂNG KÝ DỊCH VỤ (SERVICES) ---

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 1.1 Cấu hình Database
builder.Services.AddDbContext<HeriStepDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 💡 ĐÃ FIX: Logic tự động "Quay xe" tùy môi trường để không bị Crash do Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

if (builder.Environment.IsDevelopment())
{
    // Ở máy Local (Dev): Dùng RAM máy tính để code cho lẹ, không bị văng lỗi nếu lười bật Redis
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSignalR();
}
else
{
    // Trên Server (Production): Bắt buộc xài Redis để chịu tải & đồng bộ nhiều máy chủ
    if (!string.IsNullOrEmpty(redisConnectionString))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
        });

        builder.Services.AddSignalR().AddStackExchangeRedis(redisConnectionString, options => {
            options.Configuration.AbortOnConnectFail = false; // Chống crash khi Redis giật lag
        });
    }
}

// 1.2 Đăng ký các Services phụ trợ (Clean Architecture)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStallService, StallService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>(); // Thay thế AwsS3 bằng Local tạm

builder.Services.AddHostedService<DailyTourUpdateService>();
builder.Services.AddHttpClient<TranslationService>();

// 1.4 JWT Authentication
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"] ?? "superSecretKey_NeedToChange_InProduction_123456789");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// 1.5 CẤU HÌNH CORS (Sửa lại đoạn này để SignalR không bị chặn)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // 💡 Thay cho AllowAnyOrigin để SignalR chấp nhận
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // 💡 BẮT BUỘC có dòng này thì SignalR mới kết nối được
    });
});

var app = builder.Build();

// --- 2. CẤU HÌNH MIDDLEWARE (Pipeline - Cực kỳ quan trọng) ---
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseStaticFiles();

// 💡 Serve uploaded files từ thư mục UploadedFiles (nằm NGOÀI wwwroot để tránh Hot Reload crash)
var uploadedFilesPath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
if (!Directory.Exists(uploadedFilesPath))
    Directory.CreateDirectory(uploadedFilesPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadedFilesPath),
    RequestPath = ""
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// --- 3. ĐỊNH TUYẾN (ENDPOINTS) ---
app.MapControllers();
app.MapHub<StallHub>("/stallHub");

app.Run();