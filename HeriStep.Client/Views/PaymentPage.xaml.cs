using System;
using Microsoft.Maui.Controls;
using HeriStep.Client.Services;

namespace HeriStep.Client.Views
{
    /// <summary>
    /// PaymentPage — demo checkout screen.
    /// Price is passed from the caller (SubscriptionPage / RenewalPage).
    /// All UI strings are localized via L.Get().
    /// </summary>
    public partial class PaymentPage : ContentPage
    {
        private string _selectedMethod = string.Empty;

        // ── Bindable price passed from outside ──────────────────────────────
        public string PackageName { get; private set; } = "3-Day Silver Pass";
        public string PackageTier { get; private set; } = "SILVER TIER";
        public decimal PackagePrice { get; private set; } = 12.00m;
        public string CurrencySymbol { get; private set; } = "$";
        /// <summary>Formatted price string, e.g. "$12.00"</summary>
        public string FormattedPrice => $"{CurrencySymbol}{PackagePrice:F2}";
        public string FormattedTotal => FormattedPrice;
        public string FormattedSubtotal => FormattedPrice;
        public string FeeText => $"{CurrencySymbol}0.00";

        // ── Localized labels ─────────────────────────────────────────────────
        public string LblTitle          => L.Get("payment_title");
        public string LblOrderSummary   => L.Get("payment_order_summary");
        public string LblMethodHeader   => L.Get("payment_method_label");
        public string LblApplePay       => L.Get("payment_apple_pay");
        public string LblApplePayDesc   => L.Get("payment_apple_pay_desc");
        public string LblCard           => L.Get("payment_card");
        public string LblCardDesc       => L.Get("payment_card_desc");
        public string LblMomo           => L.Get("payment_momo");
        public string LblMomoDesc       => L.Get("payment_momo_desc");
        public string LblSubtotal       => L.Get("payment_subtotal");
        public string LblFee            => L.Get("payment_fee");
        public string LblTotal          => L.Get("payment_total");
        public string LblCta            => L.Get("payment_cta");
        public string LblSecure         => L.Get("payment_secure");

        // ── Constructors ─────────────────────────────────────────────────────

        /// <summary>Default: shows blank demo prices.</summary>
        public PaymentPage()
        {
            InitializeComponent();
            _selectedMethod = L.Get("payment_apple_pay");
            BindingContext = this;
        }

        /// <summary>Use this constructor to pass real package data.</summary>
        public PaymentPage(string packageName, decimal price,
                           string tier = "SILVER TIER", string currency = "$")
        {
            InitializeComponent();
            PackageName     = packageName;
            PackagePrice    = price;
            PackageTier     = tier;
            CurrencySymbol  = currency;
            _selectedMethod = L.Get("payment_apple_pay");
            BindingContext  = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Offline check — warn user but don't block (payment intent was already selected)
            if (!ConnectivityService.IsOnline)
            {
                ConnectivityService.CheckAndAlert(this);
            }
            ApplyLocalization();
        }

        // ── Localization ─────────────────────────────────────────────────────
        private void ApplyLocalization()
        {
            Title = L.Get("payment_title");
            OnPropertyChanged(null); // refresh all bound strings
        }

        // ── Back ─────────────────────────────────────────────────────────────
        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            try { await Navigation.PopAsync(); }
            catch { }
        }

        // ── Checkout ─────────────────────────────────────────────────────────
        private async void OnCheckoutClicked(object sender, EventArgs e)
        {
            // Require internet for real payment
            if (!ConnectivityService.CheckAndAlert(this)) return;

            var msg = string.Format(L.Get("payment_selected_method"), _selectedMethod)
                      + "\n" + L.Get("payment_demo_msg");
            await DisplayAlert(L.Get("payment_title"), msg, L.Get("close"));
        }

        // ── Method selection ─────────────────────────────────────────────────
        private void OnAppleMethodTapped(object sender, EventArgs e)
        {
            _selectedMethod = L.Get("payment_apple_pay");
            SetMethodHighlight(appleSelected: true, cardSelected: false, momoSelected: false);
        }

        private void OnCardMethodTapped(object sender, EventArgs e)
        {
            _selectedMethod = L.Get("payment_card");
            SetMethodHighlight(appleSelected: false, cardSelected: true, momoSelected: false);
        }

        private void OnMomoMethodTapped(object sender, EventArgs e)
        {
            _selectedMethod = L.Get("payment_momo");
            SetMethodHighlight(appleSelected: false, cardSelected: false, momoSelected: true);
        }

        private void SetMethodHighlight(bool appleSelected, bool cardSelected, bool momoSelected)
        {
            appleMethodFrame.BorderColor = appleSelected ? Color.FromArgb("#FF5722") : Colors.Transparent;
            cardMethodFrame.BorderColor  = cardSelected  ? Color.FromArgb("#FF5722") : Colors.Transparent;
            momoMethodFrame.BorderColor  = momoSelected  ? Color.FromArgb("#FF5722") : Colors.Transparent;
        }
    }
}
