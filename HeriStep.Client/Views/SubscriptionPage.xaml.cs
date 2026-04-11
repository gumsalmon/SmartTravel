using HeriStep.Client.Services;
using HeriStep.Shared.Models;
using Microsoft.Maui.Controls;

namespace HeriStep.Client.Views
{
    public partial class SubscriptionPage : ContentPage
    {
        private readonly SubscriptionService _subscriptionService;
        private TicketPackage _selectedPackage;

        public SubscriptionPage(SubscriptionService subscriptionService)
        {
            InitializeComponent();
            _subscriptionService = subscriptionService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            lblDeviceId.Text = _subscriptionService.GetDeviceId();
            
            await TranslateUI();

            var packages = await _subscriptionService.GetPackagesAsync();
            if (packages != null)
            {
                string lang = L.CurrentLanguage;
                foreach (var pkg in packages)
                {
                    // Map back to standard names and then translate
                    string baseName = pkg.Id switch {
                        1 => "Gói Khám Phá Nhanh (2 Giờ)",
                        2 => "Gói Trải Nghiệm Tiêu Chuẩn (24 Giờ)",
                        3 => "Gói Bản Địa Không Giới Hạn (1 Tuần)",
                        _ => pkg.PackageName
                    };
                    
                    if (lang == "en")
                    {
                        pkg.PackageName = pkg.Id switch {
                            1 => "Quick Discovery Package (2 Hours)",
                            2 => "Standard Experience Package (24 Hours)",
                            3 => "Unlimited Local Package (1 Week)",
                            _ => baseName
                        };
                    }
                    else if (lang == "vi")
                    {
                        pkg.PackageName = baseName;
                    }
                    else
                    {
                        pkg.PackageName = await TranslationService.TranslateTextAsync(baseName, lang);
                    }
                }
                BindableLayout.SetItemsSource(vlPackages, packages);
            }
        }

        private async Task TranslateUI()
        {
            string lang = L.CurrentLanguage;

            // Dùng hardcode fallback tạm thời nếu Google Dịch trên giả lập bị chặn (Lưu lượng 429 Quota)
            if (lang == "en")
            {
                Title = "Select Subscription";
                lblDescription.Text = "Your device is not activated or has expired. Please select a package.";
                lblDevicePrefix.Text = "Device ID:";
                lblQrTitle.Text = "Scan the QR code below to pay:";
                lblQrNote.Text = "Payment description is already filled!";
                lblWait.Text = "Waiting for payment...";
                btnCancel.Text = "Go Back";
            }
            else
            {
                Title = await TranslationService.TranslateTextAsync("Đăng ký gói cước", lang);
                lblDescription.Text = await TranslationService.TranslateTextAsync("Thiết bị của bạn chưa được kích hoạt hoặc đã hết hạn. Vui lòng chọn gói cước.", lang);
                lblDevicePrefix.Text = await TranslationService.TranslateTextAsync("Mã thiết bị:", lang);
                lblQrTitle.Text = await TranslationService.TranslateTextAsync("Quét mã QR dưới đây để thanh toán:", lang);
                lblQrNote.Text = await TranslationService.TranslateTextAsync("Nội dung nạp đã được nhập sẵn!", lang);
                lblWait.Text = await TranslationService.TranslateTextAsync("Đang chờ nhận tiền tít tít...", lang);
                btnCancel.Text = await TranslationService.TranslateTextAsync("Trở lại", lang);
            }
        }

        private void OnPackageTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is TicketPackage package)
            {
                _selectedPackage = package;
                ShowPaymentQR(package);
            }
        }

        private bool _isPolling = false;
        private string _currentOrderId = "";

        private async void ShowPaymentQR(TicketPackage package)
        {
            vlPackages.IsVisible = false;
            vlPayment.IsVisible = true;
            
            // Lấy QR thật từ Backend SePay
            var result = await _subscriptionService.PurchasePackageAsync(package.Id);
            
            if (result != null && result.Success && !string.IsNullOrEmpty(result.QrUrl) && !string.IsNullOrEmpty(result.OrderId))
            {
                // Force Download and set Image Source on UI Thread to ensure render
                try {
                    var httpClient = new HttpClient();
                    string absoluteQrUrl = result.QrUrl.StartsWith("http") ? result.QrUrl : AppConstants.BaseApiUrl + result.QrUrl;
                    var bytes = await httpClient.GetByteArrayAsync(absoluteQrUrl);
                    
                    MainThread.BeginInvokeOnMainThread(() => {
                        imgQrCode.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
                        lblPaymentAmount.Text = (L.CurrentLanguage == "en") ? $"Total: {package.Price:N0} VND" : $"Tổng tiền: {package.Price:N0} đ";
                        lblQrNote.Text = (L.CurrentLanguage == "en") ? "Payment description is pre-filled!" : "Nội dung nạp đã được nhập sẵn!";
                        lblQrNote.TextColor = Colors.Orange;
                    });
                } catch (Exception ex) {
                     MainThread.BeginInvokeOnMainThread(() => {
                        // HIỂN THỊ LỖI THẲNG LÊN MÀN HÌNH ĐỂ DEBUG
                        lblQrNote.Text = "ERR: " + ex.Message + " | URL: " + result.QrUrl;
                        lblQrNote.TextColor = Colors.Red;
                     });
                }
                
                // Kích hoạt cơ chế Polling (3 giây/lần)
                _isPolling = true;
                _currentOrderId = result.OrderId;
                StartPaymentPolling();
            }
            else
            {
                await DisplayAlert("Lỗi", "Không thể tạo mã thanh toán. Vui lòng thử lại sau.", "Đóng");
                vlPackages.IsVisible = true;
                vlPayment.IsVisible = false;
            }
        }

        private void StartPaymentPolling()
        {
            Dispatcher.StartTimer(TimeSpan.FromSeconds(3), () =>
            {
                if (!_isPolling) return false; // Stop timer

                Task.Run(async () =>
                {
                    bool isPaid = await _subscriptionService.CheckPaymentStatusAsync(_currentOrderId);
                    if (isPaid)
                    {
                        _isPolling = false; // Dừng Polling

                        // Cập nhật lại offline cache
                        await _subscriptionService.CheckStatusAsync();

                        Dispatcher.Dispatch(async () =>
                        {
                            await DisplayAlert("Tuyệt vời!", "Thanh toán thành công. App đã mở khóa!", "Vào App");
                            Application.Current.MainPage = new AppShell();
                        });
                    }
                });

                return _isPolling; // Return true để timer chạy tiếp
            });
        }

        private void OnCancelPayment(object sender, EventArgs e)
        {
            _isPolling = false; // Ngừng hỏi thăm Backend
            vlPackages.IsVisible = true;
            vlPayment.IsVisible = false;
        }
    }
}
