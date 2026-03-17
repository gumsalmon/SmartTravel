using Microsoft.EntityFrameworkCore;
using HeriStep.API.Data;
using HeriStep.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<HeriStep.API.Services.DailyTourUpdateService>();


// 1. Cấu hình Database (Giữ nguyên)
builder.Services.AddDbContext<HeriStep.API.Data.HeriStepDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// 💡 THÊM DÒNG NÀY: Đăng ký TranslationService có dùng HttpClient
builder.Services.AddHttpClient<HeriStep.API.Services.TranslationService>();

// --- SỬA BƯỚC 3: Đăng ký dịch vụ CORS ---
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin()   // Cho phép tất cả các nguồn (Web Admin, App...)
              .AllowAnyMethod()   // Cho phép GET, POST, PUT, DELETE
              .AllowAnyHeader();  // Cho phép mọi loại Header
    });
});
// --------------------------------------------------

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// --- SỬA BƯỚC 3: Kích hoạt CORS Middleware ---
// QUAN TRỌNG: Dòng này phải nằm TRƯỚC UseAuthorization và MapControllers
app.UseCors("AllowAll");
// -------------------------------------------------------

app.UseStaticFiles(); // Cho phép truy cập ảnh trong wwwroot/images

app.UseAuthorization();

app.MapControllers();

app.Run();