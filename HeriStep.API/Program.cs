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

// 1.1 Cấu hình Database & Redis
builder.Services.AddDbContext<HeriStepDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// 1.2 Đăng ký các Services phụ trợ (Clean Architecture)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStallService, StallService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>(); // Thay thế AwsS3 bằng Local tạm

builder.Services.AddHostedService<DailyTourUpdateService>();
builder.Services.AddHttpClient<TranslationService>();

// 1.3 KHAI BÁO SIGNALR & Redis Backplane
builder.Services.AddSignalR().AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis"));

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

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// --- 3. ĐỊNH TUYẾN (ENDPOINTS) ---
app.MapControllers();
app.MapHub<StallHub>("/stallHub");

app.Run();