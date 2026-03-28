using Android.App;
using Android.Content.PM;
using Android.OS;

namespace HeriStep.Client
{
    // Bỏ qua SplashTheme bị lỗi của MAUI, ép điện thoại nhận cấu hình Material Components nguyên thủy để sửa triệt để thanh Menu Toolbar (BottomNavigationView)
    [Activity(Theme = "@style/Theme.MaterialComponents.DayNight.NoActionBar", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}
