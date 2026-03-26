using HeriStep.Shared.Models;

namespace HeriStep.Client.Views;

public partial class ShopDetailPage : ContentPage
{
    // Bắt buộc phải có 1 biến Stall truyền vào đây
    public ShopDetailPage(Stall shop)
    {
        InitializeComponent();

        // Ẩn cái thanh Navigation mặc định xấu xí đi
        NavigationPage.SetHasNavigationBar(this, false);

        // PHÉP THUẬT DATA BINDING: Gắn toàn bộ dữ liệu của quán vào Giao Diện!
        BindingContext = shop;
    }

    private async void BackButton_Clicked(object sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1) { await Navigation.PopAsync(); }
        else { await Shell.Current.GoToAsync(".."); }
    }

    private async void PlayAudio_Clicked(object sender, EventArgs e)
    {
        var stall = BindingContext as Stall;
        if (stall == null) return;

        try
        {
            // Lấy preference language đã lưu, mặc định tiếng Việt
            string lang = Microsoft.Maui.Storage.Preferences.Default.Get("lang_code", "vi");
            using var client = new System.Net.Http.HttpClient();
            var url = $"http://10.0.2.2:5297/api/Stalls/{stall.Id}/tts/{lang}";
            
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonDocument.Parse(json);
                string textToSpeech = result.RootElement.GetProperty("text").GetString() ?? "";

                // Gọi hàm lõi AI Native của thiết bị
                await TextToSpeech.Default.SpeakAsync(textToSpeech);
            }
            else 
            {
                await Application.Current.Windows[0].Page.DisplayAlert("Lỗi", "Không tìm thấy nội dung audio", "OK");
            }
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TTS Error: {ex.Message}");
        }
    }
}