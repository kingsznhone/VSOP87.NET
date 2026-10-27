using System.Runtime.Intrinsics;
using System.Text;

namespace VSOP87.Tests;

/// <summary>
/// Every hardware-accelerated SIMD width must reproduce the scalar path.
/// </summary>
[TestClass]
public class SimdConsistencyTests
{
    private const double Tolerance = 1e-12;

    // Float paths sum in a different order; coordinates reach ~30 au (float ulp ~2e-6) and sums span ~1e3 terms.
    private const double FloatTolerance = 1e-4;

    private static readonly int[] _years = [1000, 1600, 1900, 2000, 2024, 2100, 2500, 3000];

    public static IEnumerable<object[]> SimdModes()
    {
        if (Vector128.IsHardwareAccelerated)
        {
            yield return [SimdMode.Vector128];
        }
        if (Vector256.IsHardwareAccelerated)
        {
            yield return [SimdMode.Vector256];
        }
        if (Vector512.IsHardwareAccelerated)
        {
            yield return [SimdMode.Vector512];
        }
    }

    [TestMethod]
    [DynamicData(nameof(SimdModes))]
    public void DoubleSimdMatchesScalar(SimdMode mode)
    {
        Calculator scalar = new(SimdMode.Scalar);
        Calculator simd = new(mode);
        Assert.AreEqual(mode, simd.Mode);

        Compare((b, v, t) => scalar.GetPlanetPosition(b, v, t), (b, v, t) => simd.GetPlanetPosition(b, v, t), Tolerance, mode.ToString());
    }

    [TestMethod]
    [DynamicData(nameof(SimdModes))]
    public void FloatSimdMatchesScalar(SimdMode mode)
    {
        CalculatorF scalar = new(SimdMode.Scalar);
        CalculatorF simd = new(mode);
        Assert.AreEqual(mode, simd.Mode);

        Compare((b, v, t) => scalar.GetPlanetPosition(b, v, t), (b, v, t) => simd.GetPlanetPosition(b, v, t), FloatTolerance, $"{mode} (float)");
    }

    [TestMethod]
    public void AutoPicksWidestAcceleratedWidth()
    {
        SimdMode expected = Vector512.IsHardwareAccelerated ? SimdMode.Vector512
            : Vector256.IsHardwareAccelerated ? SimdMode.Vector256
            : Vector128.IsHardwareAccelerated ? SimdMode.Vector128
            : SimdMode.Scalar;

        Assert.AreEqual(expected, new Calculator().Mode);
        Assert.AreEqual(expected, new CalculatorF().Mode);
    }

    private static void Compare(
        Func<VSOPBody, VSOPVersion, VSOPTime, VSOPResult> expectedFunc,
        Func<VSOPBody, VSOPVersion, VSOPTime, VSOPResult> actualFunc,
        double tolerance,
        string name)
    {
        StringBuilder failures = new();
        int cases = 0;
        double max = 0d;
        foreach ((VSOPVersion version, VSOPBody body) in TestData.AllPairs())
        {
            foreach (int year in _years)
            {
                cases++;
                VSOPTime time = new(new DateTime(year, 6, 15, 12, 0, 0, DateTimeKind.Utc), TimeFrame.UTC);
                ReadOnlySpan<double> expected = expectedFunc(body, version, time).Variables;
                ReadOnlySpan<double> actual = actualFunc(body, version, time).Variables;
                for (int i = 0; i < expected.Length; i++)
                {
                    double diff = TestData.Diff(version, i, expected[i], actual[i]);
                    max = Math.Max(max, diff);
                    if (diff > tolerance)
                    {
                        failures.AppendLine($"{version} {body} {year} [{i}]: diff {diff:R}.");
                    }
                }
            }
        }

        Assert.AreEqual(0, failures.Length, $"{name} differs from Scalar in some of {cases} cases (max {max:E3}):{Environment.NewLine}{failures}");
    }
}
