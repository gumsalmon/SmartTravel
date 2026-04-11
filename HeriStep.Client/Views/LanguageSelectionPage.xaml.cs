using System;
using Microsoft.Maui.Controls;
using HeriStep.Client.Services;

namespace HeriStep.Client.Views
{
    public partial class LanguageSelectionPage : ContentPage
    {
        private readonly SubscriptionService _subscriptionService;

        public LanguageSelectionPage(SubscriptionService subscriptionService)
        {
            InitializeComponent();
            _subscriptionService = subscriptionService;
        }

        private void OnLanguageSelected(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string lang)
            {
                // Save selected language globally
                L.SetLanguage(lang);

                // Move to LoadingPage to continue with Subscription Check
                Application.Current.MainPage = new LoadingPage(_subscriptionService);
            }
        }
    }
}
