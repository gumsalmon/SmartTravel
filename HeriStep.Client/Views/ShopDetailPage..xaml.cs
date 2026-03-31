using System;
using HeriStep.Shared.Models;
using Microsoft.Maui.Controls;

namespace HeriStep.Client.Views
{
    public partial class ShopDetailPage : ContentPage
    {
        private Stall _stall;

        public ShopDetailPage(Stall stall)
        {
            InitializeComponent();
            _stall = stall;

            if (stall != null)
            {
                // Update basic info from stall
                shopName.Text = string.IsNullOrEmpty(stall.Name) ? "Signature\nExperience" : stall.Name.Replace(" ", "\n");
                
                if (!string.IsNullOrEmpty(stall.ImageUrl))
                {
                    heroImage.Source = stall.ImageUrl;
                }
            }
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