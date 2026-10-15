using System.Globalization;

namespace HouseholdBudget.Services;

/// <summary>
/// Formats money the same way everywhere in the app.
/// The dollar sign is written into the format on purpose. The built-in currency format
/// depends on the server's language setting, which shows a generic symbol on the Linux host.
/// </summary>
public static class Money
{
    /// <summary>Returns an amount like $1,234.50, or -$10.00 for a negative amount.</summary>
    public static string Format(decimal amount) =>
        amount.ToString("$#,##0.00", CultureInfo.InvariantCulture);
}
