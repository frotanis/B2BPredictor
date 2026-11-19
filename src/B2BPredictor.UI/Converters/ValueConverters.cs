using System.Globalization;
using B2BPredictor.Application.Contracts;

namespace B2BPredictor.UI.Converters;

internal static class RiskPalette
{
    public static readonly Color Low = Color.FromArgb("#2E7D32");
    public static readonly Color Medium = Color.FromArgb("#EF6C00");
    public static readonly Color High = Color.FromArgb("#C62828");
    public static readonly Color Unknown = Color.FromArgb("#757575");

    // Ті самі пороги, що й у PredictionService (>= 75 — низький, >= 45 — помірний).
    public static Color FromProbability(double probability) => probability switch
    {
        >= 75 => Low,
        >= 45 => Medium,
        _ => High
    };
}

/// <summary>Ймовірність успіху або кредитний рейтинг (0..100) → колір рівня ризику.</summary>
public sealed class ProbabilityToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            double d => RiskPalette.FromProbability(d),
            int i => RiskPalette.FromProbability(i),
            float f => RiskPalette.FromProbability(f),
            _ => RiskPalette.Unknown
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Текст рівня ризику з API ("Низький ризик" / "Помірний ризик" / "Високий ризик") → колір.</summary>
public sealed class RiskLevelToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string ?? string.Empty;
        if (text.Contains("Низьк", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Low", StringComparison.OrdinalIgnoreCase))
        {
            return RiskPalette.Low;
        }

        if (text.Contains("Помірн", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Medium", StringComparison.OrdinalIgnoreCase))
        {
            return RiskPalette.Medium;
        }

        if (text.Contains("Висок", StringComparison.OrdinalIgnoreCase)
            || text.Contains("High", StringComparison.OrdinalIgnoreCase))
        {
            return RiskPalette.High;
        }

        return RiskPalette.Unknown;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>OrderStatus → підпис українською.</summary>
public sealed class OrderStatusToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        OrderStatus.Pending => "Очікує",
        OrderStatus.Approved => "Схвалено",
        OrderStatus.Rejected => "Відхилено",
        OrderStatus.Completed => "Виконано",
        _ => "—"
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>true, якщо статус замовлення дорівнює значенню ConverterParameter (наприклад "Pending").</summary>
public sealed class OrderStatusEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is OrderStatus status
        && parameter is string expected
        && Enum.TryParse<OrderStatus>(expected, ignoreCase: true, out var parsed)
        && status == parsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>true, якщо значення не null (і не порожній рядок/колекція).</summary>
public sealed class IsNotNullOrEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        null => false,
        string s => !string.IsNullOrWhiteSpace(s),
        System.Collections.ICollection c => c.Count > 0,
        _ => true
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Список рядків → один рядок через " · ".</summary>
public sealed class StringListToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is IEnumerable<string> items ? string.Join(" · ", items) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
