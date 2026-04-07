using System;
using HeriStep.Client.Services;
using HeriStep.Shared.Models;
using Microsoft.Maui.Controls;

namespace HeriStep.Client.Views
{
    public partial class ShopDetailPage : ContentPage
    {
        private Stall _stall;

        public ShopDetailPage(Stall stall)
        {
            Console.WriteLine("[CRITICAL_LOG] ShopDetailPage Constructor started...");
            try
            {
                InitializeComponent();
                _stall = stall;

                if (stall != null)
                {
                    shopName.Text = string.IsNullOrEmpty(stall.Name) 
                        ? "Signature Experience" 
                        : stall.Name;

                    if (!string.IsNullOrEmpty(stall.ImageUrl))
                    {
                        heroImage.Source = stall.ImageUrl;
                    }
                    else
                    {
                        string[] localFoods = { "pho_bo.jpg", "banh_mi.jpg", "oc_len.jpg", "bun_bo_hue.jpg", 
                                                "goi_cuon.jpg", "hu_tieu.jpg", "banh_xeo.jpg", "che_ba_mau.jpg", 
                                                "ca_phe_trung.jpg", "com_tam.jpg" };
                        int index = Math.Abs(stall.Id) % localFoods.Length;
                        heroImage.Source = localFoods[index];
                    }
                }

                ApplyLocalization();
                L.LanguageChanged += () => MainThread.BeginInvokeOnMainThread(ApplyLocalization);
                Console.WriteLine("[CRITICAL_LOG] ShopDetailPage Constructor finished successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL_LOG] ShopDetailPage FATAL INSTANTIATION CRASH: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void ApplyLocalization()
        {
            lblShopAddress.Text = L.Get("shop_address");
            lblMenuTitle.Text = L.Get("shop_menu_title");
            lblMenuDesc.Text = L.Get("shop_menu_desc");
            
            lblDish1Desc.Text = L.Get("shop_dish1_desc");
            lblDish2Desc.Text = L.Get("shop_dish2_desc");
            lblDish3Desc.Text = L.Get("shop_dish3_desc");
            
            lblDish1Tag1.Text = L.Get("shop_tag_musttry");
            lblDish1Tag2.Text = L.Get("shop_tag_spicy");
            
            btnOrder1.Text = "🛒  " + L.Get("shop_add_order");
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            try { await Navigation.PopAsync(); }
            catch { }
        }

        private async void OnOrderClicked(object sender, EventArgs e)
        {
            await DisplayAlert(L.Get("notification"), L.Get("shop_order_alert"), L.Get("ok"));
        }

        private async void OnFeatureComingSoon(object sender, EventArgs e)
        {
            await DisplayAlert(L.Get("coming_soon"), L.Get("shop_feature_soon"), L.Get("ok"));
        }
    }
}