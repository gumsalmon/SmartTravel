using System;
using Microsoft.Maui.Storage;

namespace HeriStep.Client.Views
{
    public partial class LanguagePage : ContentPage
    {
        public LanguagePage()
        {
            InitializeComponent();
        }

        private async void OnLanguageSelected(object sender, TappedEventArgs e)
        {
            try
            {
                // 💡 FIX CỰC KỲ KỸ: Lấy langCode từ cả e.Parameter hoặc CommandParameter
                string langCode = e.Parameter as string;
                if (string.IsNullOrEmpty(langCode))
                {
                    // 💡 FIX: Microsoft.Maui.Controls.View mới có GestureRecognizers
                    var view = sender as View;
                    var tap = view?.GestureRecognizers.OfType<TapGestureRecognizer>().FirstOrDefault();
                    langCode = tap?.CommandParameter as string;
                }

                if (string.IsNullOrEmpty(langCode)) langCode = "en"; // Cuối cùng mới là fallback English
                
                // Lưu cài đặt
                Preferences.Default.Set("user_language", langCode);
                Preferences.Default.Set("has_selected_language", true);
                Preferences.Default.Set("voice_speed", 1.2f);
                Preferences.Default.Set("voice_radius", 50.0);

                // Khởi tạo lại App
                Application.Current.MainPage = new AppShell();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Lỗi cài đặt ngôn ngữ: " + ex.Message, "OK");
            }
        }
    }
}
