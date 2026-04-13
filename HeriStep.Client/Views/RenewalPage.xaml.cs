using HeriStep.Client.Services;
using HeriStep.Shared.Models;
using Microsoft.Maui.Controls;

namespace HeriStep.Client.Views
{
    public partial class RenewalPage : ContentPage
    {
        private readonly SubscriptionService _subscriptionService;
        private TicketPackage _selectedPackage;
        private bool _isPolling = false;
        private string _currentOrderId = "";

        public RenewalPage(SubscriptionService subscriptionService)
        {
            InitializeComponent();
            _subscriptionService = subscriptionService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Áp dụng Đa Ngôn Ngữ
            pageRoot.Title = L.Get("renew_title");
            lblRenewExpired.Text = L.Get("renew_expired");
            lblDevicePrefix.Text = L.Get("renew_device");
            lblChoosePackage.Text = L.Get("renew_choose_pkg");
            lblQrTitle.Text = L.Get("renew_scan_qr");
            lblQrNote.Text = L.Get("renew_note");
            lblWait.Text = L.Get("renew_waiting");
            btnCancel.Text = L.Get("renew_back");

            // Hiển thị Device ID
            lblDeviceId.Text = ShortenDeviceId(_subscriptionService.GetDeviceId());

            // Hiển thị thông tin hết hạn từ local cache
            var offlineExpiryStr = Microsoft.Maui.Storage.Preferences.Default.Get("sub_expires_at", "");
            if (DateTime.TryParse(offlineExpiryStr, out DateTime expiry) && expiry != default)
            {
                lblExpiryInfo.Text = (L.CurrentLanguage == "en") 
                    ? $"Package expired at {expiry.ToLocalTime():dd/MM/yyyy HH:mm}." 
                    : $"Gói đã hết hạn vào {expiry.ToLocalTime():dd/MM/yyyy HH:mm}.";
            }
            else
            {
                lblExpiryInfo.Text = L.Get("renew_info");
            }

            // Load danh sách gói cước từ API
            await LoadPackagesAsync();
        }

        private async Task LoadPackagesAsync()
        {
            aiLoad.IsVisible = true;
            aiLoad.IsRunning = true;
            vlPackages.IsVisible = false;

            try
            {
                var packages = await _subscriptionService.GetPackagesAsync();
                if (packages != null && packages.Count > 0)
                {
                    string lang = L.CurrentLanguage;
                    foreach (var pkg in packages)
                    {
                        string baseName = pkg.Id switch
                        {
                            1 => "Gói Khám Phá Nhanh (2 Giờ)",
                            2 => "Gói Trải Nghiệm Tiêu Chuẩn (24 Giờ)",
                            3 => "Gói Bản Địa Không Giới Hạn (1 Tuần)",
                            _ => pkg.PackageName
                        };

                        pkg.PackageName = lang == "en" ? pkg.Id switch
                        {
                            1 => "Quick Discovery Package (2 Hours)",
                            2 => "Standard Experience Package (24 Hours)",
                            3 => "Unlimited Local Package (1 Week)",
                            _ => baseName
                        } : baseName;
                    }

                    BindableLayout.SetItemsSource(vlPackages, packages);
                    vlPackages.IsVisible = true;
                }
                else
                {
                    await DisplayAlert("Lỗi", "Không thể tải danh sách gói cước. Vui lòng kiểm tra kết nối.", "Thử lại");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RENEWAL] LoadPackages error: {ex.Message}");
                await DisplayAlert("Lỗi kết nối", "Không thể kết nối máy chủ. Vui lòng thử lại.", "OK");
            }
            finally
            {
                aiLoad.IsRunning = false;
                aiLoad.IsVisible = false;
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

        private async void ShowPaymentQR(TicketPackage package)
        {
            // Chuyển sang section thanh toán
            vlPackages.IsVisible = false;
            aiLoad.IsVisible = false;
            vlPayment.IsVisible = true;

            lblSelectedPackageName.Text = package.PackageName;
            lblPaymentAmount.Text = (L.CurrentLanguage == "en")
                ? $"Total: {package.Price:N0} VND"
                : $"Tổng tiền: {package.Price:N0} đ";

            // Gọi API tạo QR mới
            var result = await _subscriptionService.PurchasePackageAsync(package.Id);

            if (result != null && result.Success && !string.IsNullOrEmpty(result.QrUrl) && !string.IsNullOrEmpty(result.OrderId))
            {
                // Tải và hiển thị QR image
                try
                {
                    var httpClient = new HttpClient();
                    string absoluteQrUrl = result.QrUrl.StartsWith("http")
                        ? result.QrUrl
                        : AppConstants.BaseApiUrl + result.QrUrl;

                    var bytes = await httpClient.GetByteArrayAsync(absoluteQrUrl);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        imgQrCode.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
                        lblQrNote.Text = (L.CurrentLanguage == "en")
                            ? "✅ Payment description is pre-filled!"
                            : "✅ Nội dung chuyển khoản đã được điền sẵn!";
                        lblQrNote.TextColor = Colors.LightGreen;
                    });
                }
                catch (Exception imgEx)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        lblQrNote.Text = $"⚠️ Lỗi tải QR: {imgEx.Message}";
                        lblQrNote.TextColor = Colors.Red;
                    });
                }

                // Bắt đầu Polling kiểm tra thanh toán (mỗi 3 giây)
                _isPolling = true;
                _currentOrderId = result.OrderId;
                StartPaymentPolling();
            }
            else
            {
                await DisplayAlert("Lỗi", "Không thể tạo mã thanh toán. Vui lòng thử lại.", "Đóng");
                vlPackages.IsVisible = true;
                vlPayment.IsVisible = false;
            }
        }

        /// <summary>
        /// Polling mỗi 3 giây — GET /api/Payments/check-status/{orderId}
        /// Khi isPaid=true: cập nhật local cache và mở AppShell.
        /// </summary>
        private void StartPaymentPolling()
        {
            Dispatcher.StartTimer(TimeSpan.FromSeconds(3), () =>
            {
                if (!_isPolling) return false;

                Task.Run(async () =>
                {
                    bool isPaid = await _subscriptionService.CheckPaymentStatusAsync(_currentOrderId);
                    if (isPaid)
                    {
                        _isPolling = false;

                        // Cập nhật lại local cache ExpiryDate từ server sau khi gia hạn thành công
                        var freshStatus = await _subscriptionService.CheckStatusAsync();

                        Dispatcher.Dispatch(async () =>
                        {
                            string msg = (L.CurrentLanguage == "en")
                                ? "Payment successful! Your package has been renewed."
                                : "Thanh toán thành công! Gói cước đã được gia hạn.";
                            await DisplayAlert("🎉 Gia hạn thành công!", msg, "Vào App");
                            Application.Current.MainPage = new AppShell();
                        });
                    }
                });

                return _isPolling;
            });
        }

        private void OnCancelPayment(object sender, EventArgs e)
        {
            _isPolling = false;
            vlPayment.IsVisible = false;
            vlPackages.IsVisible = true;
        }

        // Rút gọn Device ID để hiển thị trên UI (ví dụ: 3c21...9f2a)
        private string ShortenDeviceId(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId) || deviceId.Length <= 8) return deviceId;
            return deviceId.Substring(0, 4) + "..." + deviceId.Substring(deviceId.Length - 4);
        }
    }
}
