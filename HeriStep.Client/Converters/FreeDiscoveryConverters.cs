using System.Globalization;

namespace HeriStep.Client.Converters
{
    /// <summary>Đảo ngược bool: true → false, false → true (dùng cho IsVisible)</summary>
    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && !b;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && !b;
    }

    /// <summary>IsRunning → Text nút Start/Stop</summary>
    public class BoolToStartStopConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? "⏹ Dừng Khám Phá" : "▶ Bắt đầu Khám Phá";

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>IsRunning → màu nút (đỏ khi đang chạy, xanh khi dừng)</summary>
    public class BoolToButtonColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true
                ? Color.FromArgb("#DC2626")   // red-600 — đang chạy
                : Color.FromArgb("#2563EB");  // blue-600 — chờ

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
