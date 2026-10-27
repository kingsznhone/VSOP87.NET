namespace VSOP87.Tests;

/// <summary>
/// Public API behavior of <see cref="Calculator"/> and <see cref="CalculatorF"/>: construction, results, secular terms and threading.
/// </summary>
[TestClass]
public class CalculatorTests
{
    private static Calculator _full = null!;

    private static readonly VSOPTime[] _times =
    [
        new(new DateTime(1100, 3, 1, 0, 0, 0, DateTimeKind.Utc), TimeFrame.TDB),
        new(new DateTime(1850, 7, 4, 6, 0, 0, DateTimeKind.Utc), TimeFrame.TDB),
        new(TestData.J2000, TimeFrame.TDB),
        new(new DateTime(2024, 10, 28, 18, 30, 0, DateTimeKind.Utc), TimeFrame.UTC),
        new(new DateTime(2900, 12, 31, 0, 0, 0, DateTimeKind.Utc), TimeFrame.TDB),
    ];

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _full = new Calculator(SimdMode.Scalar);
    }

    [TestMethod]
    [DataRow(VSOPVersion.VSOP87, VSOPBody.EARTH)]
    [DataRow(VSOPVersion.VSOP87, VSOPBody.SUN)]
    [DataRow(VSOPVersion.VSOP87A, VSOPBody.SUN)]
    [DataRow(VSOPVersion.VSOP87B, VSOPBody.EMB)]
    [DataRow(VSOPVersion.VSOP87E, VSOPBody.EMB)]
    [DataRow((VSOPVersion)42, VSOPBody.EARTH)]
    [DataRow(VSOPVersion.VSOP87A, (VSOPBody)42)]
    public void UnavailableBodyThrows(VSOPVersion version, VSOPBody body)
    {
        VSOPTime time = _times[2];
        Assert.ThrowsExactly<ArgumentException>(() => _full.GetPlanetPosition(body, version, time));
        Assert.ThrowsExactly<ArgumentException>(() => new CalculatorF(SimdMode.Scalar).GetPlanetPosition(body, version, time));
    }

    [TestMethod]
    public void ResultsCarryVersionMetadata()
    {
        CalculatorF fast = new();
        foreach ((VSOPVersion version, VSOPBody body) in TestData.AllPairs())
        {
            foreach (VSOPResult result in new[] { _full.GetPlanetPosition(body, version, _times[3]), fast.GetPlanetPosition(body, version, _times[3]) })
            {
                Assert.AreEqual(body, result.Body);
                Assert.AreEqual(Utility.GetCoordinatesType(version), result.CoordinatesType);
                Assert.AreEqual(Utility.GetFrameType(version), result.FrameType);
                Assert.AreEqual(Utility.GetEpoch(version), result.Epoch);
                Assert.AreEqual(6, result.Variables.Length);

                int angle = TestData.AngleIndex(version);
                for (int i = 0; i < 6; i++)
                {
                    Assert.IsTrue(double.IsFinite(result.Variables[i]), $"{version} {body} [{i}]");
                }
                if (angle >= 0)
                {
                    Assert.IsTrue(result.Variables[angle] is >= 0d and < Math.Tau, $"{version} {body} angle {result.Variables[angle]}");
                }
                if (version == VSOPVersion.VSOP87)
                {
                    Assert.IsTrue(result.Variables[0] > 0d, "Semi-major axis must be positive.");
                }
            }
        }
    }

    /// <summary>
    /// The secular polynomial of every longitude (mean motion reduced modulo 2pi) and its rate
    /// must match an independent 28-digit decimal evaluation.
    /// </summary>
    [TestMethod]
    public void SecularPolynomialIsEvaluatedToFullPrecision()
    {
        const decimal TicksPerMillennium = 315576000000000000m;
        const decimal Tau = 6.2831853071795864769252867666m;
        long j2000Ticks = TestData.J2000.Ticks;

        VSOPTime[] times =
        [
            new(new DateTime(1001, 2, 3, 4, 5, 6, DateTimeKind.Utc).AddTicks(1234567), TimeFrame.TDB),
            new(new DateTime(1987, 6, 5, 4, 3, 2, DateTimeKind.Utc), TimeFrame.TDB),
            new(new DateTime(2999, 12, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(9999999), TimeFrame.TDB),
        ];

        int checkedCount = 0;
        System.Text.StringBuilder failures = new();
        foreach (PlanetTable planet in TestData.LoadRaw())
        {
            int angle = TestData.AngleIndex(planet.Version);
            if (angle < 0)
            {
                continue;
            }

            PowerTable[] powers = planet.variables[(VSOPVariable)(angle + 1)].PowerTables;
            decimal[] secular = new decimal[powers.Length];
            double[] hi = new double[powers.Length];
            double[] lo = new double[powers.Length];
            for (int it = 0; it < powers.Length; it++)
            {
                Term[] terms = powers[it]?.Terms ?? [];
                VSOPData.SplitSecular(terms, it, hi, lo);
                foreach (Term term in terms)
                {
                    if (term.C == 0d)
                    {
                        // B is 0 or pi, so cos(B) is exactly +-1.
                        secular[it] += Exact(term.A) * (decimal)Math.Cos(term.B);
                    }
                }
            }

            foreach (VSOPTime time in times)
            {
                decimal t = (time.TDB.Ticks - j2000Ticks) / TicksPerMillennium;
                decimal value = 0m;
                decimal rate = 0m;
                decimal tPow = 1m;
                for (int it = 0; it < secular.Length; it++)
                {
                    value += secular[it] * tPow;
                    if (it + 1 < secular.Length)
                    {
                        rate += (it + 1) * secular[it + 1] * tPow;
                    }
                    tPow *= t;
                }
                value -= decimal.Floor(value / Tau) * Tau;
                double expectedRate = (double)rate;

                double td = VSOPData.Millennia(time, out double tError);
                (double actualValue, double actualRate) = VSOPData.Secular(hi, lo, td, tError, reduce: true);

                double diff = TestData.Diff(planet.Version, angle, (double)value, VSOPData.NormalizeAngle(actualValue));
                // A few ulp of 2pi.
                if (diff > 4e-15)
                {
                    failures.AppendLine($"{planet.Version} {planet.Body} {time.TDB:o}: longitude diff {diff:E3}");
                }
                double rateDiff = Math.Abs(actualRate - expectedRate);
                if (rateDiff > 1e-15 * Math.Abs(expectedRate))
                {
                    failures.AppendLine($"{planet.Version} {planet.Body} {time.TDB:o}: rate diff {rateDiff:E3}");
                }
                checkedCount++;
            }
        }

        // VSOP87, VSOP87B and VSOP87D bodies x times.
        Assert.AreEqual((8 + 8 + 8) * times.Length, checkedCount);
        Assert.AreEqual(0, failures.Length, failures.ToString());

        // (decimal)double keeps only ~15 digits; go through a 28-digit string instead.
        static decimal Exact(double x) => decimal.Parse(x.ToString("E27", System.Globalization.CultureInfo.InvariantCulture), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
    }

    [TestMethod]
    public async Task AsyncMatchesSync()
    {
        CalculatorF fast = new();
        VSOPTime time = _times[3];

        VSOPResult expected = _full.GetPlanetPosition(VSOPBody.JUPITER, VSOPVersion.VSOP87D, time);
        VSOPResult actual = await _full.GetPlanetPositionAsync(VSOPBody.JUPITER, VSOPVersion.VSOP87D, time);
        CollectionAssert.AreEqual(expected.Variables.ToArray(), actual.Variables.ToArray());

        VSOPResult expectedF = fast.GetPlanetPosition(VSOPBody.JUPITER, VSOPVersion.VSOP87D, time);
        VSOPResult actualF = await fast.GetPlanetPositionAsync(VSOPBody.JUPITER, VSOPVersion.VSOP87D, time);
        CollectionAssert.AreEqual(expectedF.Variables.ToArray(), actualF.Variables.ToArray());
    }

    [TestMethod]
    public void ConcurrentCallsMatchSequential()
    {
        Calculator simd = new();
        (VSOPVersion Version, VSOPBody Body)[] pairs = TestData.AllPairs().ToArray();
        double[][] expected = pairs.Select(p => simd.GetPlanetPosition(p.Body, p.Version, _times[1]).Variables.ToArray()).ToArray();

        double[][] actual = new double[pairs.Length][];
        Parallel.For(0, pairs.Length, i =>
        {
            actual[i] = simd.GetPlanetPosition(pairs[i].Body, pairs[i].Version, _times[1]).Variables.ToArray();
        });

        for (int i = 0; i < pairs.Length; i++)
        {
            CollectionAssert.AreEqual(expected[i], actual[i], $"{pairs[i].Version} {pairs[i].Body}");
        }
    }
}
