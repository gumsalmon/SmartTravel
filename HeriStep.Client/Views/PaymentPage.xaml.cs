using System;
using Microsoft.Maui.Controls;

namespace HeriStep.Client.Views
{
    public partial class PaymentPage : ContentPage
    {
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
    }
}
