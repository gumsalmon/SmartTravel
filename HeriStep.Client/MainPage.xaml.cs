using System;
using HeriStep.Client.Views;

namespace HeriStep.Client
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void BtnMap_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("MapPage"); // Assuming MapPage has a route registered
            // wait, in Shell, we might need to navigate differently. Let's just push for now if route fails.
             try {
                var mapPage = new MapPage();
                await Navigation.PushAsync(mapPage);
            } catch { }
        }

        private async void BtnShop_Clicked(object sender, EventArgs e)
        {
            try
            {
                // mock stall ID 1 to open ShopDetail
                var stall = new HeriStep.Shared.Models.Stall { Id = 1, Name = "Ốc Oanh Vĩnh Khánh", ImageUrl = "https://images.unsplash.com/photo-1548690312-e3b507d8c110?w=600" };
                await Navigation.PushAsync(new ShopDetailPage(stall));
            }
            catch { }
        }
    }
}