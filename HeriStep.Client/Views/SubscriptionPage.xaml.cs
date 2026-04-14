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
            TranslateUI();

            var packages = await _subscriptionService.GetPackagesAsync();
            if (packages != null)
            {
                foreach (var pkg in packages)
                {
                    // 💡 Lấy tên gói dựa trên ID từ LocalizationService (pkg_name_1, pkg_name_2...)
                    pkg.PackageName = L.Get($"pkg_name_{pkg.Id}");
                }
                BindableLayout.SetItemsSource(vlPackages, packages);
            }
        }

        private void TranslateUI()
        {
            Title = L.Get("sub_title");
            lblDescription.Text = L.Get("sub_desc");
            lblDevicePrefix.Text = L.Get("sub_device_id");
            lblQrTitle.Text = L.Get("sub_qr_title");
            lblQrNote.Text = L.Get("sub_qr_note");
            lblWait.Text = L.Get("sub_wait");
            btnCancel.Text = L.Get("sub_cancel");
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
                        lblPaymentAmount.Text = string.Format(L.Get("sub_total_amount"), package.Price.ToString("N0"), "đ");
                        lblQrNote.Text = L.Get("sub_qr_note");
                        lblQrNote.TextColor = Colors.Orange;
                    });
                } catch (Exception ex) {
                     MainThread.BeginInvokeOnMainThread(() => {
                        lblQrNote.Text = "ERR: " + ex.Message;
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
                await DisplayAlert(L.Get("aura_error_title"), L.Get("alert_payment_error_msg"), L.Get("close"));
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
                            await DisplayAlert(L.Get("alert_payment_success_title"), L.Get("alert_payment_success_msg"), L.Get("btn_enter_app"));
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
