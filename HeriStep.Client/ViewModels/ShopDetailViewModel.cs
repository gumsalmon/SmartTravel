using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Diagnostics;
using HeriStep.Client.Services;
using HeriStep.Shared.Models;

namespace HeriStep.Client.ViewModels;

public class ShopDetailViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabaseService _localDb;
    private readonly AudioTranslationService _audioService;
    private readonly Stopwatch _stopwatch = new();
    private readonly Stall _currentStall;

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

    private bool _isTtsPlaying;
    public ICommand PlayAudioCommand { get; }

    public ShopDetailViewModel(LocalDatabaseService localDb, AudioTranslationService audioService, Stall stall)
    {
        _localDb = localDb;
        _audioService = audioService;
        _currentStall = stall;
        ApplyLocalization();
        L.LanguageChanged += OnLanguageChanged;

        PlayAudioCommand = new Command(async () => await PlayAudioAsync());
    }

    private async Task PlayAudioAsync()
    {
        if (_isTtsPlaying || _currentStall == null) return;
        _isTtsPlaying = true;
        _stopwatch.Restart();

        try
        {
            var lang = L.CurrentLanguage;
            string? textToSpeak = await _audioService.GetStallScriptAsync(_currentStall.Id, lang);
            if (string.IsNullOrWhiteSpace(textToSpeak))
            {
                textToSpeak = string.Format(L.Get("audio_welcome_stall"), _currentStall.Name);
            }
            await _audioService.SpeakAsync(textToSpeak, lang);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SHOP_DETAIL_VM] PlayAudio Error: {ex.Message}");
        }
        finally
        {
            _isTtsPlaying = false;
            StopAndSaveDuration();
        }
    }

    private void StopAndSaveDuration()
    {
        if (_stopwatch.IsRunning)
        {
            _stopwatch.Stop();
        }

        try
        {
            int elapsed = (int)_stopwatch.Elapsed.TotalSeconds;
            if (elapsed > 0 && _currentStall != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await _localDb.UpdateLatestStallVisitDurationAsync(_currentStall.Id, elapsed);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SHOP_DETAIL_VM] Failed to save duration: {ex.Message}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SHOP_DETAIL_VM] StopAndSaveDuration Error: {ex.Message}");
        }
        finally
        {
            _stopwatch.Reset();
        }
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
        StopAndSaveDuration();
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
