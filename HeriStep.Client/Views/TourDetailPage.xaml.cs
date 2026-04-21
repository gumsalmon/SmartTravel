using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using HeriStep.Shared.Models;
using HeriStep.Client.Services;

namespace HeriStep.Client.Views
{
    public partial class TourDetailPage : ContentPage
    {
        private readonly Tour _tour;

        public static readonly BindableProperty NavigateBtnTextProperty =
            BindableProperty.Create(nameof(NavigateBtnText), typeof(string), typeof(TourDetailPage), "Chỉ Đường");

        public string NavigateBtnText
        {
            get => (string)GetValue(NavigateBtnTextProperty);
            set => SetValue(NavigateBtnTextProperty, value);
        }

        public TourDetailPage(Tour tour)
        {
            InitializeComponent();
            _tour = tour;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Language
            pageRoot.Title = L.Get("tour_detail_title");
            lblStallsTitle.Text = L.Get("tour_stalls_title");
            NavigateBtnText = L.Get("tour_navigate_btn");

            // Info
            lblTourTitle.Text = _tour.TourName;
            lblTourDesc.Text = _tour.Description;
            if (!string.IsNullOrEmpty(_tour.ImageUrl))
            {
                imgTourBanner.Source = _tour.ImageUrl;
            }

            await LoadStallsAsync();
        }

        private async Task LoadStallsAsync()
        {
            aiLoad.IsVisible = true;
            aiLoad.IsRunning = true;
            vlContent.IsVisible = false;

            try
            {
                using var client = new HttpClient();
                var uri = $"{AppConstants.BaseApiUrl}/api/Tours/{_tour.Id}";
                var response = await client.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    var tourData = await response.Content.ReadFromJsonAsync<Tour>();
                    if (tourData?.Stalls != null && tourData.Stalls.Count > 0)
                    {
                        foreach (var stall in tourData.Stalls)
                        {
                            if (!string.IsNullOrEmpty(stall.ImageUrl) && !stall.ImageUrl.StartsWith("http"))
                            {
                                stall.ImageUrl = AppConstants.BaseApiUrl + stall.ImageUrl;
                            }
                            else if (string.IsNullOrEmpty(stall.ImageUrl))
                            {
                                stall.ImageUrl = "https://images.unsplash.com/photo-1544025162-8e658402afb0?w=600";
                            }
                        }

                        BindableLayout.SetItemsSource(vlStallsList, tourData.Stalls);
                    }
                    else
                    {
                        lblStallsTitle.Text = "Không có quán ăn nào đang hoạt động trong lộ trình này.";
                    }
                }
                else
                {
                    lblStallsTitle.Text = "Không thể tải danh sách sạp. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TOUR_DETAIL] Fetch Error: {ex.Message}");
                lblStallsTitle.Text = "Lỗi kết nối. Vui lòng thử lại sau.";
            }
            finally
            {
                aiLoad.IsRunning = false;
                aiLoad.IsVisible = false;
                vlContent.IsVisible = true;
            }
        }

        private async void OnNavigateClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Stall stall)
            {
                try
                {
                    var location = new Microsoft.Maui.Devices.Sensors.Location(stall.Latitude, stall.Longitude);
                    var options = new MapLaunchOptions 
                    { 
                        Name = stall.Name 
                    };
                    
                    // Launch Native Map App (Google Maps / Apple Maps)
                    await Map.Default.OpenAsync(location, options);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Lỗi", $"Không thể mở bản đồ chỉ đường: {ex.Message}", "OK");
                }
            }
        }
    }
}
