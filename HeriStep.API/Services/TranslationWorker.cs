using HeriStep.API.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace HeriStep.API.Services
{
    public class TranslationWorker : BackgroundService
    {
        private readonly ILogger<TranslationWorker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public TranslationWorker(ILogger<TranslationWorker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Translation Worker đã khởi động và đang chờ việc...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 💡 TECH LEAD FIX: Tạo Scope mới cho mỗi mẻ để chống tràn Connection Pool
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<HeriStepDbContext>();
                        var translationService = scope.ServiceProvider.GetRequiredService<TranslationService>();

                        // ==========================================================
                        // 1. XỬ LÝ LỜI CHÀO CỦA SẠP (StallContents)
                        // ==========================================================
                        var pendingContents = await context.StallContents
                            .Where(c => !c.IsProcessed && !c.IsDeleted && c.LangCode != "vi") // Không dịch tiếng Việt
                            .Take(10) // Quét từng mẻ 10 dòng
                            .ToListAsync(stoppingToken);

                        foreach (var content in pendingContents)
                        {
                            try
                            {
                                // Lấy bản gốc Tiếng Việt
                                var viContent = await context.StallContents
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(c => c.StallId == content.StallId && c.LangCode == "vi" && !c.IsDeleted, stoppingToken);

                                if (viContent != null && !string.IsNullOrWhiteSpace(viContent.TtsScript))
                                {
                                    _logger.LogInformation($"[Stall {content.StallId}] Đang dịch TTS sang {content.LangCode}...");
                                    var translatedText = await translationService.TranslateTextAsync(viContent.TtsScript, content.LangCode, "vi");

                                    // CƠ CHẾ FALLBACK
                                    if (!string.IsNullOrWhiteSpace(translatedText) && !translatedText.Contains("MYMEMORY WARNING"))
                                    {
                                        content.TtsScript = translatedText;
                                    }
                                    else
                                    {
                                        content.TtsScript = viContent.TtsScript + " (Auto-translation unavailable)";
                                        _logger.LogWarning($"[Stall {content.StallId}] API chặn, dùng Fallback cho {content.LangCode}");
                                    }
                                }
                                else
                                {
                                    content.TtsScript = ""; // Bản gốc trống -> Xóa trắng
                                }

                                content.IsProcessed = true;
                                content.UpdatedAt = DateTime.Now;

                                // 💡 TECH LEAD FIX: Lưu DB ngay lập tức sau khi xử lý xong 1 dòng
                                await context.SaveChangesAsync(stoppingToken);
                            }
                            catch (Exception innerEx)
                            {
                                _logger.LogWarning($"[Stall {content.StallId}] Lỗi Exception {content.LangCode}: {innerEx.Message}");
                                content.TtsScript = "[Lỗi hệ thống dịch thuật]";
                                content.IsProcessed = true;
                                content.UpdatedAt = DateTime.Now;

                                // 💡 TECH LEAD FIX: Lưu trạng thái lỗi xuống DB để không bị kẹt ở lần lặp sau
                                await context.SaveChangesAsync(stoppingToken);
                            }

                            await Task.Delay(2000, stoppingToken); // Trễ 2s chống Google ban IP
                        }

                        // ==========================================================
                        // 2. XỬ LÝ TÊN VÀ MÔ TẢ MÓN ĂN (ProductTranslations)
                        // ==========================================================
                        var pendingProducts = await context.ProductTranslations
                            .Where(p => !p.IsProcessed && !p.IsDeleted && p.LangCode != "vi")
                            .Take(10)
                            .ToListAsync(stoppingToken);

                        foreach (var product in pendingProducts)
                        {
                            try
                            {
                                // Lấy bản gốc Tiếng Việt
                                var viProduct = await context.ProductTranslations
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(p => p.ProductId == product.ProductId && p.LangCode == "vi" && !p.IsDeleted, stoppingToken);

                                if (viProduct != null && !string.IsNullOrWhiteSpace(viProduct.ProductName))
                                {
                                    _logger.LogInformation($"[Product {product.ProductId}] Đang dịch món ăn sang {product.LangCode}...");

                                    var translatedName = await translationService.TranslateTextAsync(viProduct.ProductName, product.LangCode, "vi");

                                    // Fallback cho Tên món ăn
                                    if (!string.IsNullOrWhiteSpace(translatedName) && !translatedName.Contains("MYMEMORY WARNING"))
                                    {
                                        product.ProductName = translatedName;
                                    }
                                    else
                                    {
                                        product.ProductName = viProduct.ProductName; // Gắn tạm tiếng Việt
                                    }

                                    // Dịch luôn mô tả nếu có
                                    if (!string.IsNullOrWhiteSpace(viProduct.ProductDesc))
                                    {
                                        var translatedDesc = await translationService.TranslateTextAsync(viProduct.ProductDesc, product.LangCode, "vi");
                                        if (!string.IsNullOrWhiteSpace(translatedDesc) && !translatedDesc.Contains("MYMEMORY WARNING"))
                                        {
                                            product.ProductDesc = translatedDesc;
                                        }
                                        else
                                        {
                                            product.ProductDesc = viProduct.ProductDesc;
                                        }
                                    }
                                }
                                else
                                {
                                    product.ProductName = "";
                                }

                                product.IsProcessed = true;
                                product.UpdatedAt = DateTime.Now;

                                // 💡 TECH LEAD FIX: Lưu DB ngay lập tức
                                await context.SaveChangesAsync(stoppingToken);
                            }
                            catch (Exception innerEx)
                            {
                                _logger.LogWarning($"[Product {product.ProductId}] Lỗi Exception món ăn {product.LangCode}: {innerEx.Message}");
                                product.IsProcessed = true;
                                product.UpdatedAt = DateTime.Now;

                                // 💡 TECH LEAD FIX: Lưu trạng thái lỗi xuống DB
                                await context.SaveChangesAsync(stoppingToken);
                            }

                            await Task.Delay(2000, stoppingToken);
                        }
                    } // Kết thúc khối using, DB Connection tự động được đóng và trả về Pool
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "[Fatal Error] Mất kết nối Database hoặc luồng Background bị lỗi nghiêm trọng.");
                }

                // Chờ 5 giây rồi quét lại DB vòng tiếp theo
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}