using System;
using Microsoft.Maui.Controls;

namespace HeriStep.Client.Views
{
    public partial class PaymentPage : ContentPage
    {
        private string _selectedMethod = "Apple Pay";

        public PaymentPage()
        {
            InitializeComponent();
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PopAsync();
            }
            catch { }
        }

        private async void OnCheckoutClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Thanh toán", $"Bạn đã chọn phương thức: {_selectedMethod}.\nĐây là màn hình demo, cổng thanh toán thật sẽ được nối sau.", "Đóng");
        }

        private void OnAppleMethodTapped(object sender, EventArgs e)
        {
            _selectedMethod = "Apple Pay";
            SetMethodHighlight(appleSelected: true, cardSelected: false, momoSelected: false);
        }

        private void OnCardMethodTapped(object sender, EventArgs e)
        {
            _selectedMethod = "Thẻ tín dụng/ghi nợ";
            SetMethodHighlight(appleSelected: false, cardSelected: true, momoSelected: false);
        }

        private void OnMomoMethodTapped(object sender, EventArgs e)
        {
            _selectedMethod = "Ví MoMo";
            SetMethodHighlight(appleSelected: false, cardSelected: false, momoSelected: true);
        }

        private void SetMethodHighlight(bool appleSelected, bool cardSelected, bool momoSelected)
        {
            appleMethodFrame.BorderColor = appleSelected ? Color.FromArgb("#004D40") : Colors.Transparent;
            cardMethodFrame.BorderColor = cardSelected ? Color.FromArgb("#004D40") : Colors.Transparent;
            momoMethodFrame.BorderColor = momoSelected ? Color.FromArgb("#004D40") : Colors.Transparent;
        }
    }
}
