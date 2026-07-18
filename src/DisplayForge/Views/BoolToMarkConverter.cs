using System.Globalization;
using System.Windows.Data;
using DisplayForge.Resources;

namespace DisplayForge.Views;

public sealed class BoolToMarkConverter : IValueConverter
{
    public static readonly BoolToMarkConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Strings.AppliedBadge : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
