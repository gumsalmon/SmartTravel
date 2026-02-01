var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
// 1. Cấu hình HttpClient để gọi API
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5297/") });

// 2. Kích hoạt Session
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
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
// 3. Phải có dòng UseSession trước UseAuthorization
app.UseSession();
app.UseAuthorization();

app.MapRazorPages();
app.Run();