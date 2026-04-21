using HeriStep.Client.Services;
using HeriStep.Shared.Models;
using Microsoft.Maui.Controls;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Net.Http;

namespace HeriStep.Client.Views
{
    public partial class SubscriptionPage : ContentPage
    {
        private readonly SubscriptionService _subscriptionService;
        private TicketPackage _selectedPackage;
        private bool _isPolling = false;
        private string _currentOrderId = "";

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
            }
        }

        private void TranslateUI()
        {
            pageRoot.Title = L.Get("sub_title");
            lblDescription.Text = L.Get("sub_desc");
            lblDevicePrefix.Text = L.Get("sub_device_id");
            lblQrTitle.Text = L.Get("sub_qr_title");
            lblQrNote.Text = L.Get("sub_qr_note");
            lblWait.Text = L.Get("sub_wait");
            btnCancel.Text = L.Get("sub_cancel");
            btnBypass.Text = "DEV: Bypass Payment & Enter App"; // Dev button
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
                 var dummyUi = new TicketPackageUI { Original = realPkg, PackageName = realPkg.PackageName };
                 ShowPaymentQR(dummyUi);
            }
        }

        private async void ShowPaymentQR(TicketPackageUI uiPkg)
        {
            vlPackages.IsVisible = false;
            vlPayment.IsVisible = true;
            
            lblPaymentAmount.Text = string.Format(L.Get("sub_total_amount"), uiPkg.Original.Price.ToString("N0"), "đ");

            // Lấy QR thật từ Backend SePay
            var result = await _subscriptionService.PurchasePackageAsync(uiPkg.Original.Id);
            
            if (result != null && result.Success && !string.IsNullOrEmpty(result.QrUrl) && !string.IsNullOrEmpty(result.OrderId))
            {
                try {
                    var httpClient = new HttpClient();
                    string absoluteQrUrl = result.QrUrl.StartsWith("http") ? result.QrUrl : AppConstants.BaseApiUrl + result.QrUrl;
                    var bytes = await httpClient.GetByteArrayAsync(absoluteQrUrl);
                    
                    MainThread.BeginInvokeOnMainThread(() => {
                        imgQrCode.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
                        lblQrNote.Text = L.Get("sub_qr_note");
                        lblQrNote.TextColor = Colors.Green;
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

                        // Cập nhật lại offline cache
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
            vlPackages.IsVisible = true;
            vlPayment.IsVisible = false;
        }

        private async void OnBypassClicked(object sender, EventArgs e)
        {
            // DEV BYPASS: giả lập thanh toán thành công
            _isPolling = false;
            await DisplayAlert("DEV BYPASS", "Đã bỏ qua cổng thanh toán.", "Vào App");
            Application.Current.MainPage = new AppShell();
        }
    }
}
