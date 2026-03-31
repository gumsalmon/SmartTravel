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
                else
                {
                    // 💡 BEAUTIFUL FALLBACK FOR SHOP DETAIL
                    string[] localFoods = { "pho_bo.jpg", "banh_mi.jpg", "oc_len.jpg", "bun_bo_hue.jpg", 
                                            "goi_cuon.jpg", "hu_tieu.jpg", "banh_xeo.jpg", "che_ba_mau.jpg", 
                                            "ca_phe_trung.jpg", "com_tam.jpg" };
                    int index = Math.Abs(stall.Id) % localFoods.Length;
                    heroImage.Source = localFoods[index];
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