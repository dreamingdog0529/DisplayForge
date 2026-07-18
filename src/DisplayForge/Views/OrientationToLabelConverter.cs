using System.Globalization;
using System.Windows.Data;
using DisplayForge.Resources;

namespace DisplayForge.Views;

/// <summary>Maps orientation degrees (0/90/180/270) to a localized label.</summary>
public sealed class OrientationToLabelConverter : IValueConverter
{
    public static readonly OrientationToLabelConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            90 => Strings.OrientationPortrait,
            180 => Strings.OrientationLandscapeFlipped,
            270 => Strings.OrientationPortraitFlipped,
            _ => Strings.OrientationLandscape
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
