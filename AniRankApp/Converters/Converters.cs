using System.Globalization;

namespace AniRankApp.Converters;

/// <summary>Returns true when the bound string has content (used to show error labels).</summary>
public class NotEmptyBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => !string.IsNullOrWhiteSpace(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Inverts a boolean (e.g. IsBusy -> IsEnabled).</summary>
public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}

/// <summary>"marko_05" -> "MA": the letters shown inside a user's avatar circle.</summary>
public class InitialsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var letters = (value as string ?? string.Empty).Where(char.IsLetterOrDigit).Take(2).ToArray();
        return letters.Length == 0 ? "?" : new string(letters).ToUpper(culture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
