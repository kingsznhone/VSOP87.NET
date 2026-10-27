using System.Globalization;
using System.Text;

namespace VSOP87.Tests.Reports;

/// <summary>
/// Diagnostic precision reports (no assertions). Each report samples +-1000 years around J2000
/// and compares a calculator configuration against the full-series double scalar reference,
/// for every version, body and variable. Output files are written next to the test assembly.
/// </summary>
[TestClass]
[TestCategory("Report")]
public class PrecisionReports
{
    private const int Samples = 2001;
    private const double SpanDays = 365250d;
    private const double NearDays = 36525d;
    private const double RadToArcsec = 180d * 3600d / Math.PI;

    private static Calculator _reference = null!;
    private static VSOPTime[] _times = null!;
    private static (VSOPVersion Version, VSOPBody Body)[] _groups = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _reference = new Calculator(SimdMode.Scalar);

        _times = new VSOPTime[Samples];
        for (int i = 0; i < Samples; i++)
        {
            _times[i] = new VSOPTime(TestData.J2000.AddDays(-SpanDays + 2 * SpanDays * i / (Samples - 1)), TimeFrame.TDB);
        }

        _groups = TestData.AllPairs().ToArray();
    }

    [TestMethod]
    public void DoubleSimdVsDoubleScalar()
    {
        Calculator simd = new(SimdMode.Auto);
        WriteReport($"Calculator ({simd.Mode}) vs Calculator (Scalar), full series.",
            simd.GetPlanetPosition, "Report_DoubleSimd.txt");
    }

    [TestMethod]
    [DataRow(PrecisionMode.Fast)]
    [DataRow(PrecisionMode.UltraFast)]
    public void TruncatedVsFull(PrecisionMode precision)
    {
        Calculator calculator = new(SimdMode.Auto, precision);
        WriteReport($"Calculator ({calculator.Mode}, {precision}, {VSOPData.ToleranceArcsec(precision)}\") vs Calculator (Scalar), full series.",
            calculator.GetPlanetPosition, $"Report_{precision}.txt");
    }

    [TestMethod]
    public void TermCounts()
    {
        List<PlanetTable> planets = TestData.LoadRaw();
        PrecisionMode[] modes = Enum.GetValues<PrecisionMode>();

        StringBuilder report = new();
        report.AppendLine("Secular terms (C = 0, always evaluated) and periodic terms kept per precision mode.");
        report.AppendLine($"Version  Body     Var | {"Secular",8} | {string.Join(" ", modes.Select(m => $"{m,10}"))}");

        long[] totals = new long[modes.Length + 1];
        foreach (PlanetTable planet in planets.OrderBy(p => p.Version).ThenBy(p => p.Body))
        {
            string[] labels = TestData.Labels(planet.Version);
            long[] bodyTotals = new long[modes.Length + 1];
            foreach (VariableTable variable in planet.variables.Values.OrderBy(v => v.Variable))
            {
                int iv = (int)variable.Variable - 1;
                double[] hi = new double[variable.PowerTables.Length];
                double[] lo = new double[variable.PowerTables.Length];
                Term[][] periodic = variable.PowerTables.Select((p, it) => VSOPData.SplitSecular(p?.Terms ?? [], it, hi, lo)).ToArray();
                long[] counts = new long[modes.Length + 1];
                counts[0] = variable.PowerTables.Sum(p => p?.Terms?.Count(t => t.C == 0d) ?? 0);
                for (int m = 0; m < modes.Length; m++)
                {
                    double budget = VSOPData.Budget(planet.Version, planet.Body, iv, VSOPData.ToleranceArcsec(modes[m]));
                    counts[m + 1] = VSOPData.Truncate(periodic, budget, planet.Version != VSOPVersion.VSOP87).Sum(p => p.Length);
                }
                for (int i = 0; i < counts.Length; i++)
                {
                    bodyTotals[i] += counts[i];
                    totals[i] += counts[i];
                }
                AppendCounts(report, $"{planet.Version,-8} {planet.Body,-8} {labels[iv],-3} |", counts);
            }
            AppendCounts(report, $"{planet.Version,-8} {planet.Body,-8} SUM |", bodyTotals);
        }
        AppendCounts(report, "TOTAL                 |", totals);

        Write(report, "Report_TermCounts.txt");
    }

    private void WriteReport(string title, Func<VSOPBody, VSOPVersion, VSOPTime, VSOPResult> evaluate, string fileName)
    {
        string[][] lines = new string[_groups.Length][];

        // Each (version, body) group is independent and calculators are read-only after construction.
        Parallel.For(0, _groups.Length, g =>
        {
            (VSOPVersion version, VSOPBody body) = _groups[g];
            string[] labels = TestData.Labels(version);
            int angle = TestData.AngleIndex(version);
            double[][] diffs = new double[6][];
            double[] nearMax = new double[6];
            for (int v = 0; v < 6; v++)
            {
                diffs[v] = new double[Samples];
            }

            for (int i = 0; i < Samples; i++)
            {
                ReadOnlySpan<double> expected = _reference.GetPlanetPosition(body, version, _times[i]).Variables;
                ReadOnlySpan<double> actual = evaluate(body, version, _times[i]).Variables;
                bool near = Math.Abs(_times[i].J2000) <= NearDays;
                for (int v = 0; v < 6; v++)
                {
                    double diff = TestData.Diff(version, v, expected[v], actual[v]);
                    diffs[v][i] = diff;
                    if (near)
                    {
                        nearMax[v] = Math.Max(nearMax[v], diff);
                    }
                }
            }

            lines[g] = new string[6];
            for (int v = 0; v < 6; v++)
            {
                double[] d = diffs[v];
                double mean = d.Average();
                double rms = Math.Sqrt(d.Average(x => x * x));
                Array.Sort(d);
                double max = d[^1];
                string arcsec = v == angle
                    ? string.Create(CultureInfo.InvariantCulture, $"{max * RadToArcsec,-10:E2} {nearMax[v] * RadToArcsec,-10:E2}")
                    : string.Empty;
                lines[g][v] = string.Create(CultureInfo.InvariantCulture,
                    $"{version,-8} {body,-8} {labels[v],-3} | {max,-10:E2} {mean,-10:E2} {rms,-10:E2} {d[Samples / 2],-10:E2} {d[(int)(Samples * 0.99)],-10:E2} | {nearMax[v],-10:E2} | {arcsec}");
            }
        });

        StringBuilder report = new();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{title} +-1000 y around J2000 ({Samples} samples)."));
        report.AppendLine("Distances and rectangular coordinates in au, angles in rad, rates per day, k/h/q/p unitless. Near = |t-J2000| <= 100 y.");
        report.AppendLine("Version  Body     Var | Max abs    Mean abs   RMS        P50        P99        | Near max   | Angle max\" Angle near max\"");
        foreach (string line in lines.SelectMany(l => l))
        {
            report.AppendLine(line.TrimEnd());
        }
        Write(report, fileName);
    }

    private static void AppendCounts(StringBuilder report, string label, long[] counts)
    {
        // counts[0] is the secular column.
        report.Append(label);
        report.Append($" {counts[0],8} |");
        foreach (long c in counts.Skip(1))
        {
            report.Append($" {c,10}");
        }
        report.AppendLine();
    }

    private void Write(StringBuilder report, string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, fileName);
        File.WriteAllText(path, report.ToString());
        TestContext.WriteLine(report.ToString());
        TestContext.WriteLine($"Report written to {path}");
    }
}
