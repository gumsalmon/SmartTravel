using HeriStep.Client.Services;
using HeriStep.Shared.Models;
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace HeriStep.Client.Views
{
    public class TicketPackageUI
    {
        public int Id { get; set; }
        public string PackageName { get; set; }
        public string DurationText { get; set; }
        public string PriceText { get; set; }
        public string ActionText { get; set; }
        public TicketPackage Original { get; set; }
    }

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

            // Áp dụng Đa Ngôn Ngữ (Bypass hardcoded EN/VI)
            pageRoot.Title = L.Get("renew_title");
            lblRenewExpired.Text = L.Get("renew_expired");
            lblDevicePrefix.Text = L.Get("renew_device");
            lblChoosePackage.Text = L.Get("renew_choose_pkg");
            lblQrTitle.Text = L.Get("renew_scan_qr");
            lblWait.Text = L.Get("renew_waiting");
            btnCancel.Text = L.Get("renew_back");
            btnBypass.Text = "DEV: Bypass Payment & Enter App"; // Dev only button

            // Hiển thị Device ID
            lblDeviceId.Text = ShortenDeviceId(_subscriptionService.GetDeviceId());

            // Expiry info
            var offlineExpiryStr = Microsoft.Maui.Storage.Preferences.Default.Get("sub_expires_at", "");
            if (DateTime.TryParse(offlineExpiryStr, out DateTime expiry) && expiry != default)
            {
                lblExpiryInfo.Text = L.Get("profile_expiry_expired") + $" ({expiry.ToLocalTime():dd/MM/yyyy})";
            }
            else
            {
                lblExpiryInfo.Text = L.Get("renew_info");
            }

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
                    var uiPackages = packages.Select(pkg => new TicketPackageUI
                    {
                        Id = pkg.Id,
                        PackageName = L.Get($"pkg_name_{pkg.Id}") ?? pkg.PackageName,
                        DurationText = $"⏱ {pkg.DurationHours} " + L.Get("renew_hours"),
                        PriceText = $"{pkg.Price:N0} đ",
                        ActionText = L.Get("renew_select"),
                        Original = pkg
                    }).ToList();

                    BindableLayout.SetItemsSource(vlPackages, uiPackages);
                    vlPackages.IsVisible = true;
                }
                else
                {
                    await DisplayAlert(L.Get("alert_payment_error_title"), "Lỗi tải gói", L.Get("ok"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RENEWAL] LoadPackages error: {ex.Message}");
            }
            finally
            {
                aiLoad.IsRunning = false;
                aiLoad.IsVisible = false;
            }
        }

        private void OnPackageTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is TicketPackageUI uiPkg)
            {
                _selectedPackage = uiPkg.Original;
                ShowPaymentQR(uiPkg);
            }
            else if (e.Parameter is TicketPackage realPkg)
            {
                _selectedPackage = realPkg;
                 // Handle in case parameter was bound directly, though we use TicketPackageUI 
                 var dummyUi = new TicketPackageUI { Original = realPkg, PackageName = realPkg.PackageName };
                 ShowPaymentQR(dummyUi);
            }
        }

        private async void ShowPaymentQR(TicketPackageUI uiPkg)
        {
            vlPackages.IsVisible = false;
            aiLoad.IsVisible = false;
            vlPayment.IsVisible = true;

            lblSelectedPackageName.Text = uiPkg.PackageName;
            lblPaymentAmount.Text = string.Format(L.Get("sub_total_amount"), uiPkg.Original.Price.ToString("N0"), "đ");

            var result = await _subscriptionService.PurchasePackageAsync(uiPkg.Original.Id);

            if (result != null && result.Success && !string.IsNullOrEmpty(result.QrUrl) && !string.IsNullOrEmpty(result.OrderId))
            {
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
                        lblQrNote.Text = L.Get("sub_qr_note");
                        lblQrNote.TextColor = Colors.Green;
                    });
                }
                catch (Exception imgEx)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        lblQrNote.Text = $"ERR: QR - {imgEx.Message}";
                        lblQrNote.TextColor = Colors.Red;
                    });
                }

                _isPolling = true;
                _currentOrderId = result.OrderId;
                StartPaymentPolling();
            }
            else
            {
                await DisplayAlert(L.Get("alert_payment_error_title"), L.Get("alert_payment_error_msg"), L.Get("close"));
                vlPackages.IsVisible = true;
                vlPayment.IsVisible = false;
            }
        }

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
                        await _subscriptionService.CheckStatusAsync();

                        Dispatcher.Dispatch(async () =>
                        {
                            await DisplayAlert(L.Get("alert_payment_success_title"), L.Get("alert_payment_success_msg"), L.Get("btn_enter_app"));
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

        private async void OnBypassClicked(object sender, EventArgs e)
        {
            // DEV BYPASS: giả lập thanh toán thành công
            _isPolling = false;
            await DisplayAlert("DEV BYPASS", "Đã bỏ qua cổng thanh toán.", "Vào App");
            Application.Current.MainPage = new AppShell();
        }

        private string ShortenDeviceId(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId) || deviceId.Length <= 8) return deviceId;
            return deviceId.Substring(0, 4) + "..." + deviceId.Substring(deviceId.Length - 4);
        }
    }
}
