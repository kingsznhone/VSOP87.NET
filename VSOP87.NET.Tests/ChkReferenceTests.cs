using System.Globalization;
using System.Text;

namespace VSOP87.Tests;

/// <summary>
/// Validates scalar and SIMD paths against the official VSOP87.chk reference values (500 records, all versions and bodies).
/// </summary>
[TestClass]
public class ChkReferenceTests
{
    // .chk values are printed with 10 decimals; allow one unit in the last place.
    private const double Tolerance = 1e-10;

    // CalculatorF is a visualization grade approximation; this only guards against gross errors.
    private const double FloatTolerance = 1e-2;

    private static Calculator _scalar = null!;
    private static Calculator _simd = null!;
    private static List<TestData.ChkEntry> _entries = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _scalar = new Calculator(SimdMode.Scalar);
        _simd = new Calculator(SimdMode.Auto);
        _entries = TestData.ParseChk();
    }

    [TestMethod]
    public void ChkFileCoversEveryVersionAndBody()
    {
        Assert.AreEqual(500, _entries.Count);
        foreach ((VSOPVersion version, VSOPBody body) in TestData.AllPairs())
        {
            Assert.AreEqual(10, _entries.Count(e => e.Version == version && e.Body == body), $"{version} {body}");
        }
    }

    [TestMethod]
    public void ScalarAndSimdMatchOfficialChkValues()
    {
        StringBuilder report = new();
        report.AppendLine($"Version  Body     JD(TDB)    | Scalar     | {_simd.Mode}");
        double scalarMax = 0d;
        double simdMax = 0d;
        StringBuilder failures = new();
        foreach (TestData.ChkEntry entry in _entries)
        {
            VSOPTime time = TestData.FromJulianDate(entry.JulianDate);
            ReadOnlySpan<double> scalarValues = _scalar.GetPlanetPosition(entry.Body, entry.Version, time).Variables;
            ReadOnlySpan<double> simdValues = _simd.GetPlanetPosition(entry.Body, entry.Version, time).Variables;
            double scalar = 0d;
            double simd = 0d;
            for (int i = 0; i < entry.Values.Length; i++)
            {
                double scalarDiff = TestData.Diff(entry.Version, i, entry.Values[i], scalarValues[i]);
                double simdDiff = TestData.Diff(entry.Version, i, entry.Values[i], simdValues[i]);
                if (scalarDiff > Tolerance || simdDiff > Tolerance)
                {
                    failures.AppendLine(string.Create(CultureInfo.InvariantCulture,
                        $"{entry.Version} {entry.Body} JD{entry.JulianDate:F1} [{i}]: scalar {scalarDiff:E3}, simd {simdDiff:E3}"));
                }
                scalar = Math.Max(scalar, scalarDiff);
                simd = Math.Max(simd, simdDiff);
            }
            scalarMax = Math.Max(scalarMax, scalar);
            simdMax = Math.Max(simdMax, simd);
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"{entry.Version,-8} {entry.Body,-8} {entry.JulianDate,-10:F1} | {scalar,-10:E2} | {simd,-10:E2}"));
        }
        report.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Max error: scalar {scalarMax:E3}, simd {simdMax:E3}, tolerance {Tolerance:E0}"));
        TestContext.WriteLine(report.ToString());

        Assert.AreEqual(0, failures.Length, failures.ToString());
    }

    [TestMethod]
    public void CalculatorFIsCloseToOfficialChkValues()
    {
        CalculatorF scalar = new(SimdMode.Scalar);
        CalculatorF simd = new(SimdMode.Auto);
        double max = 0d;
        string worst = string.Empty;
        foreach (TestData.ChkEntry entry in _entries)
        {
            VSOPTime time = TestData.FromJulianDate(entry.JulianDate);
            foreach (CalculatorF calculator in new[] { scalar, simd })
            {
                double diff = TestData.MaxDiff(entry.Version, entry.Values, calculator.GetPlanetPosition(entry.Body, entry.Version, time).Variables);
                if (diff > max)
                {
                    max = diff;
                    worst = $"{calculator.Mode} {entry.Version} {entry.Body} JD{entry.JulianDate}";
                }
            }
        }
        TestContext.WriteLine($"CalculatorF max error {max:E3} at {worst}");

        Assert.IsTrue(max <= FloatTolerance, $"CalculatorF max error {max:E3} at {worst}");
    }
}
