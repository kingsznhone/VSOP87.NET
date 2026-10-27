using System.Globalization;
using System.IO.Compression;
using MemoryPack;

namespace VSOP87.Tests;

/// <summary>
/// Shared helpers: official VSOP87.chk parser, raw data loader, variable labels and angle-aware differences.
/// </summary>
internal static class TestData
{
    public static readonly DateTime J2000 = new(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Raw series as shipped in VSOP87.BR.
    /// </summary>
    public static List<PlanetTable> LoadRaw()
    {
        using FileStream fs = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "VSOP87.BR"));
        using BrotliStream bs = new(fs, CompressionMode.Decompress);
        using MemoryStream ms = new();
        bs.CopyTo(ms);
        return MemoryPackSerializer.Deserialize<List<PlanetTable>>(ms.ToArray())!;
    }

    /// <summary>
    /// One record of VSOP87.chk; <see cref="Values"/> follows the order of <see cref="VSOPResult.Variables"/>.
    /// </summary>
    public sealed record ChkEntry(VSOPVersion Version, VSOPBody Body, double JulianDate, double[] Values);

    public static VSOPTime FromJulianDate(double jd) => new(J2000.AddDays(jd - 2451545.0d), TimeFrame.TDB);

    public static IEnumerable<(VSOPVersion Version, VSOPBody Body)> AllPairs() =>
        Enum.GetValues<VSOPVersion>().SelectMany(v => Utility.ListAvailableBody(v).Select(b => (v, b)));

    /// <summary>
    /// Index of the variable reduced to [0, 2pi), or -1.
    /// </summary>
    public static int AngleIndex(VSOPVersion version) => version switch
    {
        VSOPVersion.VSOP87 => 1,
        VSOPVersion.VSOP87B or VSOPVersion.VSOP87D => 0,
        _ => -1,
    };

    public static string[] Labels(VSOPVersion version) => Utility.GetCoordinatesType(version) switch
    {
        CoordinatesType.Elliptic => ["a", "l", "k", "h", "q", "p"],
        CoordinatesType.Spherical => ["l", "b", "r", "l'", "b'", "r'"],
        _ => ["x", "y", "z", "x'", "y'", "z'"],
    };

    /// <summary>
    /// |actual - expected|, folded to [0, pi] for the angular variable.
    /// </summary>
    public static double Diff(VSOPVersion version, int index, double expected, double actual)
    {
        double diff = Math.Abs(actual - expected);
        if (index == AngleIndex(version))
        {
            diff %= Math.Tau;
            diff = Math.Min(diff, Math.Tau - diff);
        }
        return diff;
    }

    public static double MaxDiff(VSOPVersion version, ReadOnlySpan<double> expected, ReadOnlySpan<double> actual)
    {
        double max = 0d;
        for (int i = 0; i < expected.Length; i++)
        {
            max = Math.Max(max, Diff(version, i, expected[i], actual[i]));
        }
        return max;
    }

    /// <summary>
    /// Parses VSOP87.chk (header line followed by two lines of three "label value unit" triplets).
    /// </summary>
    public static List<ChkEntry> ParseChk()
    {
        string[] lines = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Resources", "VSOP87.chk"));
        List<ChkEntry> entries = [];
        for (int i = 0; i < lines.Length; i++)
        {
            string[] header = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (header.Length == 0 || !header[0].StartsWith("VSOP87", StringComparison.Ordinal))
            {
                continue;
            }

            VSOPVersion version = Enum.Parse<VSOPVersion>(header[0]);
            VSOPBody body = header[1] == "EARTH-MOON" ? VSOPBody.EMB : Enum.Parse<VSOPBody>(header[1]);
            double jd = double.Parse(header[2].AsSpan(2), CultureInfo.InvariantCulture);
            double[] first = ParseRow(lines[i + 1]);
            double[] second = ParseRow(lines[i + 2]);
            // Elliptic rows are (a, k, q) and (l, h, p); every other version is (pos x3) and (vel x3).
            double[] values = version == VSOPVersion.VSOP87
                ? [first[0], second[0], first[1], second[1], first[2], second[2]]
                : [.. first, .. second];
            entries.Add(new ChkEntry(version, body, jd, values));
            i += 2;
        }
        return entries;
    }

    private static double[] ParseRow(string line)
    {
        string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return [Parse(tokens[1]), Parse(tokens[4]), Parse(tokens[7])];

        static double Parse(string s) => double.Parse(s, CultureInfo.InvariantCulture);
    }
}
