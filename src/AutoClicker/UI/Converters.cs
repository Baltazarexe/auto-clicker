using System.Globalization;
using System.Windows.Data;

namespace AutoClicker.UI;

/// <summary>Binds an enum to RadioButtons: IsChecked = (value == ConverterParameter).</summary>
public sealed class EnumEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value?.ToString() == parameter?.ToString();

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true && parameter is string name ? Enum.Parse(targetType, name) : Binding.DoNothing;
}
