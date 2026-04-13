using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HeriStep.Client.Services;

namespace HeriStep.Client.ViewModels;

public class ShopDetailViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabaseService _localDb;

    public ObservableCollection<MenuDisplayItem> MenuItems { get; } = new();

    private string _menuEmptyText = string.Empty;
    public string MenuEmptyText
    {
        get => _menuEmptyText;
        private set
        {
            if (_menuEmptyText != value)
            {
                _menuEmptyText = value;
                OnPropertyChanged();
            }
        }
    }

    public ShopDetailViewModel(LocalDatabaseService localDb)
    {
        _localDb = localDb;
        ApplyLocalization();
        L.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        MainThread.BeginInvokeOnMainThread(ApplyLocalization);
    }

    public async Task LoadMenuItemsAsync(int stallId)
    {
        try
        {
            var menuItems = await _localDb.GetMenuItemsByStallIdAsync(stallId);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                MenuItems.Clear();
                foreach (var item in menuItems)
                {
                    MenuItems.Add(new MenuDisplayItem
                    {
                        Id = item.Id,
                        StallId = item.StallId,
                        Name = string.IsNullOrWhiteSpace(item.Name) ? L.Get("shop_menu_title") : item.Name,
                        Description = string.IsNullOrWhiteSpace(item.Description) ? L.Get("shop_menu_desc") : item.Description,
                        PriceText = item.Price > 0 ? $"{item.Price:0}k" : string.Empty,
                        ImageUrl = string.IsNullOrWhiteSpace(item.ImageUrl)
                            ? "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600"
                            : item.ImageUrl
                    });
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SHOP_DETAIL_VM] LoadMenuItemsAsync failed: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(() => MenuItems.Clear());
        }
    }

    private void ApplyLocalization()
    {
        MenuEmptyText = L.Get("profile_history").Contains("Lịch", StringComparison.OrdinalIgnoreCase)
            ? "Chưa có dữ liệu món ăn từ SQLite"
            : "No menu data from SQLite";
    }

    public void Cleanup()
    {
        L.LanguageChanged -= OnLanguageChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class MenuDisplayItem
{
    public int Id { get; set; }
    public int StallId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PriceText { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}
