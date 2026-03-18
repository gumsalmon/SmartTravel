using Microsoft.EntityFrameworkCore;
using HeriStep.API.Data;
using HeriStep.API.Services;
using HeriStep.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

// --- 1. ĐĂNG KÝ DỊCH VỤ (SERVICES) ---

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 1.1 Cấu hình Database
builder.Services.AddDbContext<HeriStepDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 1.2 Đăng ký các Services phụ trợ
builder.Services.AddHostedService<DailyTourUpdateService>();
builder.Services.AddHttpClient<TranslationService>();

// 💡 1.3 KHAI BÁO SIGNALR (Phải có để radar chạy)
builder.Services.AddSignalR();

// 💡 1.4 CẤU HÌNH CORS (Sửa lại đoạn này để SignalR không bị chặn)
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.SetIsOriginAllowed(_ => true) // 💡 Thay cho AllowAnyOrigin để SignalR chấp nhận
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // 💡 BẮT BUỘC có dòng này thì SignalR mới kết nối được
    });
});

var app = builder.Build();

// --- 2. CẤU HÌNH MIDDLEWARE (Pipeline - Cực kỳ quan trọng) ---

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 💡 2.1 CORS PHẢI ĐỨNG ĐẦU TIÊN
app.UseCors("AllowAll");

// 💡 2.2 TỚI FILE TĨNH (ẢNH)
app.UseStaticFiles();

// 💡 2.3 RỒI MỚI TỚI ROUTING
app.UseRouting();

// 💡 2.4 CUỐI CÙNG LÀ AUTH
app.UseAuthorization();

// --- 3. ĐỊNH TUYẾN (ENDPOINTS) ---

app.MapControllers();

// 💡 3.1 MAP HUB (Radar bắt khách)
app.MapHub<StallHub>("/stallHub");

app.Run();