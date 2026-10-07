namespace Bahoto.Application.Common;

public static class ServicePriceIncreaseHelper
{
    public static bool TryApply(ref decimal? value, decimal factor, out bool skipped)
    {
        skipped = false;
        if (value is null)
        {
            skipped = true;
            return false;
        }

        var next = Math.Round(value.Value * factor, 2, MidpointRounding.AwayFromZero);
        if (next < 0)
        {
            next = 0;
        }

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
