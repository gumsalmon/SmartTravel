using HeriStep.Client.ViewModels;

namespace HeriStep.Client.Views
{
    public partial class FreeDiscoveryPage : ContentPage
    {
        public FreeDiscoveryPage(FreeDiscoveryViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Khi user navigate ra khỏi page → dừng service để tiết kiệm pin
            // (optional: comment out nếu muốn tiếp tục chạy khi minimize)
            if (BindingContext is FreeDiscoveryViewModel vm && vm.IsRunning)
            {
                _ = vm.StopDiscoveryCommand.ExecuteAsync(null);
            }
        }
    }
}
