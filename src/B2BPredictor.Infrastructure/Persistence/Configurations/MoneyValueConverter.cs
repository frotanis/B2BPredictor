using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace B2BPredictor.Infrastructure.Persistence.Configurations;

public static class MoneyValueConverter
{
    public static ValueConverter<decimal, long> Instance { get; } = new(
        value => (long)Math.Round(value * 100m, MidpointRounding.AwayFromZero),
        value => value / 100m);
}
