using Microsoft.EntityFrameworkCore;
using HeriStep.API.Data;
using HeriStep.API.Services;
using HeriStep.API.Hubs;
using HeriStep.API.Interfaces;
using HeriStep.API.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// ==========================================================
// 1. ĐĂNG KÝ DỊCH VỤ (SERVICES)
// ==========================================================

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// --- 1.1 Cấu hình Database (💡 LỚP BẢO VỆ SỐ 1: Bắt lỗi ConnectionString) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("=====================================================");
    Console.WriteLine("🛑 LỖI NGHIÊM TRỌNG: KHÔNG ĐỌC ĐƯỢC CONNECTION STRING!");
    Console.WriteLine("Hãy kiểm tra lại file appsettings.json và appsettings.Development.json");
    Console.WriteLine("=====================================================");
    Console.ResetColor();

    throw new InvalidOperationException("Không tìm thấy chuỗi kết nối 'DefaultConnection' trong file cấu hình!");
}

builder.Services.AddDbContext<HeriStepDbContext>(options =>
    options.UseSqlServer(connectionString));


// --- 1.2 Cấu hình Cache & SignalR (💡 LỚP BẢO VỆ SỐ 2: Fallback Redis) ---
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

if (builder.Environment.IsDevelopment())
{
    // Ở máy Local (Dev): Dùng RAM máy tính để code cho lẹ
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSignalR();
}
else
{
    // Trên Server (Production): Ưu tiên xài Redis
    if (!string.IsNullOrWhiteSpace(redisConnectionString))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
        });

        builder.Services.AddSignalR().AddStackExchangeRedis(redisConnectionString, options =>
        {
            options.Configuration.AbortOnConnectFail = false; // Chống crash app khi Redis bị giật lag
        });
    }
    else
    {
        // LỐI THOÁT HIỂM: Nếu quên cấu hình Redis trên Server thì tạm rớt về xài RAM
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("⚠️ CẢNH BÁO: Không tìm thấy cấu hình Redis ở Production. Đang Fallback về Memory Cache!");
        Console.ResetColor();

        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSignalR();
    }
}


// --- 1.3 Đăng ký các Services phụ trợ (Clean Architecture) ---
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStallService, StallService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>(); // Thay thế AwsS3 bằng Local tạm

builder.Services.AddHostedService<DailyTourUpdateService>();
builder.Services.AddHttpClient<TranslationService>();
builder.Services.AddHostedService<TranslationWorker>();
builder.Services.AddMemoryCache(); // 💡 Thêm dòng này

// --- 1.4 JWT Authentication ---
var jwtKeyString = builder.Configuration["Jwt:Key"] ?? "superSecretKey_NeedToChange_InProduction_123456789";
var key = Encoding.ASCII.GetBytes(jwtKeyString);

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


// --- 1.5 CẤU HÌNH CORS (Mở cửa cho SignalR) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // 💡 Bắt buộc cho SignalR (thay cho AllowAnyOrigin)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // 💡 BẮT BUỘC để gửi token/cookie qua Socket
    });
});

var app = builder.Build();

// ==========================================================
// 2. CẤU HÌNH MIDDLEWARE (Pipeline)
// ==========================================================
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseStaticFiles();

// 💡 Cấu hình Serve uploaded files (Tránh Hot Reload crash)
var uploadedFilesPath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
if (!Directory.Exists(uploadedFilesPath))
{
    Directory.CreateDirectory(uploadedFilesPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadedFilesPath),
    RequestPath = ""
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ==========================================================
// 3. ĐỊNH TUYẾN (ENDPOINTS)
// ==========================================================
app.MapControllers();
app.MapHub<StallHub>("/stallHub");

app.Run();