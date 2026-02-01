var builder = WebApplication.CreateBuilder(args);

// 1. ĐĂNG KÝ HTTPCLIENT: Dòng này để Web Admin gọi được vào API
// Thay cổng 5297 bằng cổng thực tế của HeriStep.API nếu khác
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5297/")
});

// Add services to the container.
builder.Services.AddRazorPages();

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

app.UseAuthorization();

app.MapRazorPages();

app.Run();