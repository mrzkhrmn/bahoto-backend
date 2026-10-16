namespace Bahoto.Application.Common;

public static class ServicePriceIncreaseHelper
{
    public static decimal RoundUpToNearest50(decimal value)
    {
        if (value <= 0)
        {
            return 0;
        }

        return Math.Ceiling(value / 50m) * 50m;
    }

    public static bool TryApply(ref decimal? value, decimal factor, out bool skipped)
    {
        skipped = false;
        if (value is null)
        {
            skipped = true;
            return false;
        }

        var next = RoundUpToNearest50(value.Value * factor);

        if (next == value.Value)
        {
            skipped = true;
            return false;
        }

        value = next;
        return true;
    }

    public static bool HasAnyPrice(params decimal?[] values) => values.Any(v => v.HasValue);

    public static bool PricesChanged(params (decimal? Before, decimal? After)[] pairs) =>
        pairs.Any(p => p.Before != p.After);
}
