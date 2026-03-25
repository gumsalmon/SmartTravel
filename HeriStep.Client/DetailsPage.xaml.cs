using HeriStep.Shared.Models;

namespace HeriStep.Client
{
    public partial class DetailsPage : ContentPage
    {
        public DetailsPage(Stall stall)
        {
            InitializeComponent();

            // ĐÃ SỬA CHỮ 'point' THÀNH 'stall' Ở ĐÂY
            BindingContext = stall;
        }
    }
}