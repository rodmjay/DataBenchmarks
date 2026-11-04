using System.Globalization;

namespace DataBenchmarks.Reporting;

public static class Format
{
    public static string Time(double ns) => ns switch
    {
        < 1_000 => $"{ns:F0} ns",
        < 1_000_000 => $"{ns / 1_000:F1} µs",
        < 1_000_000_000 => $"{ns / 1_000_000:F2} ms",
        _ => $"{ns / 1_000_000_000:F2} s",
    };

    public static string Bytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024):F1} MB",
    };

    public static string Throughput(double opsPerSecond) => opsPerSecond switch
    {
        < 1_000 => $"{opsPerSecond:F0}/s",
        < 1_000_000 => $"{opsPerSecond / 1_000:F1}k/s",
        _ => $"{opsPerSecond / 1_000_000:F2}M/s",
    };

    public static string Ratio(double ratio) => ratio.ToString("0.00", CultureInfo.InvariantCulture) + "×";
}
