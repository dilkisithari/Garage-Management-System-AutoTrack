using System.Globalization;

namespace AutoTrack.Models.Reports;

/// <summary>Small formatting helpers so every report shows money and dates the same way.</summary>
public static class ReportFormat
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Lkr(decimal amount) =>
        (amount < 0 ? "-LKR " : "LKR ") + Math.Abs(amount).ToString("N0", Inv);

    public static string Number(decimal amount) => amount.ToString("N0", Inv);
    public static string Hours(double hours) => hours.ToString("N1", Inv);
    public static string Percent(decimal value) => value.ToString("0.0", Inv) + "%";
    public static string Date(DateTime date) => date.ToString("dd MMM yyyy", Inv);
    public static string Month(DateTime date) => date.ToString("MMM yyyy", Inv);
    public static string MonthLong(DateTime date) => date.ToString("MMMM yyyy", Inv);
    public static string Raw(decimal amount) => amount.ToString("0.##", Inv);   // for CSV export cells
}
