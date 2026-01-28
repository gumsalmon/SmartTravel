using HeriStep.Client.ViewModels;

namespace HeriStep.Client
{
    public partial class MainPage : ContentPage
    {
        // Nhận HttpClient từ hệ thống và truyền cho ViewModel
        public MainPage(HttpClient httpClient)
        {
            InitializeComponent();
            // Gắn ViewModel vào giao diện
            BindingContext = new HomeViewModel(httpClient);
        }
    }
}