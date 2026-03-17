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

            // 1. Gán thẳng cái viewModel có sẵn vào giao diện (Xóa luôn dòng new bị lỗi đỏ)
            BindingContext = viewModel;

            // 2. Lưu lại vào biến cục bộ để xài cho các hàm khác ở dưới (Hết luôn lỗi vàng)
            _viewModel = viewModel;
        }
    }
}