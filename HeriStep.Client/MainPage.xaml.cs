using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Mapsui;
using Mapsui.Projections;
using HeriStep.Shared;
using System.Net.Http.Json;
using Mapsui.Layers;
using Mapsui.Styles;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HeriStep.Client.ViewModels;

namespace HeriStep.Client
{
    public partial class MainPage : ContentPage
    {
        // Sử dụng private field để dễ dàng gọi lại ViewModel ở các hàm khác
        private readonly HomeViewModel _viewModel;

        // SỬA: Inject trực tiếp HomeViewModel thay vì HttpClient
        public MainPage(HomeViewModel viewModel)
        {
            InitializeComponent();

            // 1. Gán thẳng cái viewModel có sẵn vào giao diện
            BindingContext = viewModel;

            // 2. Lưu lại vào biến cục bộ
            _viewModel = viewModel;
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            // Nút làm mới
            _viewModel.LoadDataCommand.Execute(null);
        }

        private async void OnViewDetailsClicked(object sender, EventArgs e)
        {
            // Tạm thời hiển thị alert khi bấm xem chi tiết ở cửa hàng
            await DisplayAlert("Thông báo", "Chức năng xem chi tiết đang phát triển", "OK");
        }
    }
}