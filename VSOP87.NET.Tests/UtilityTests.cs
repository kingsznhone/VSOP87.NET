namespace VSOP87.Tests;

[TestClass]
public class UtilityTests
{
    private const double TightTolerance = 1e-13;

    private static Calculator _calculator = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _calculator = new Calculator();
    }

    [TestMethod]
    public void LBRtoXYZ_MatchesAnalyticalJacobian()
    {
        double l = 0.7;
        double b = -0.3;
        double r = 2.4;
        double dl = 0.012;
        double db = -0.004;
        double dr = 0.02;

        double[] actual = Utility.LBRtoXYZ([l, b, r, dl, db, dr]);
        double cosL = Math.Cos(l);
        double sinL = Math.Sin(l);
        double cosB = Math.Cos(b);
        double sinB = Math.Sin(b);
        double[] expected =
        [
            r * cosB * cosL,
            r * cosB * sinL,
            r * sinB,
            cosB * cosL * dr - r * sinB * cosL * db - r * cosB * sinL * dl,
            cosB * sinL * dr - r * sinB * sinL * db + r * cosB * cosL * dl,
            sinB * dr + r * cosB * db
        ];

        AssertComponents(expected, actual, TightTolerance);
    }

    [TestMethod]
    public void SphericalAndCartesianConversions_RoundTripPositionAndVelocity()
    {
        double[] expected = [0.8, -1.2, 0.4, 0.003, -0.007, 0.002];

        double[] actual = Utility.LBRtoXYZ(Utility.XYZtoLBR(expected));

        AssertComponents(expected, actual, TightTolerance);
    }

    [TestMethod]
    [DataRow(-25.0)]
    [DataRow(0.25)]
    [DataRow(40.0)]
    public void EllipticConversions_RoundTripWithNormalizedLongitude(double longitude)
    {
        double[] expected = [1.524, longitude, 0.08, -0.04, -0.02, 0.01];

        double[] actual = Utility.XYZtoELL(VSOPBody.MARS, Utility.ELLtoXYZ(VSOPBody.MARS, expected));

        AssertClose(expected[0], actual[0], 1e-12);
        AssertClose(NormalizeAngle(expected[1]), actual[1], 1e-12);
        for (int index = 2; index < expected.Length; index++)
        {
            AssertClose(expected[index], actual[index], 1e-12);
        }
        Assert.IsTrue(actual[1] >= 0.0 && actual[1] < Math.Tau);
    }

    [TestMethod]
    public void ReferenceFrameConversions_RoundTripPositionAndVelocity()
    {
        double[] expected = [0.8, -1.2, 0.4, 0.003, -0.007, 0.002];

        double[] actual = Utility.ICRStoDynamical(Utility.DynamicaltoICRS(expected));

        AssertComponents(expected, actual, 1e-14);
    }

    [TestMethod]
    public void EpochConversions_RoundTripPositionAndVelocity()
    {
        double[] expected = [0.8, -1.2, 0.4, 0.003, -0.007, 0.002];
        VSOPTime time = new(new DateTime(1750, 5, 1, 0, 0, 0, DateTimeKind.Utc), TimeFrame.TDB);

        double[] actual = Utility.DatetoJ2000(Utility.J2000toDate(expected, time), time);

        AssertComponents(expected, actual, 1e-13);
    }

    [TestMethod]
    [DataRow(2451545.0, 2000, 1, 1, 12)]
    [DataRow(2415020.0, 1899, 12, 31, 12)]
    [DataRow(2378495.0, 1799, 12, 30, 12)]
    [DataRow(2816787.5, 3000, 1, 1, 0)]
    public void JulianDateConversions_AreContinuousBefore1900(double jd, int year, int month, int day, int hour)
    {
        // VSOP87.chk dates, including those before the OADate discontinuity of 1899-12-30.
        DateTime tdb = new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

        Assert.AreEqual(jd, VSOPTime.ToJulianDate(tdb));
        Assert.AreEqual(jd - 2451545.0, VSOPTime.ToJ2000(tdb));
        Assert.AreEqual(tdb, VSOPTime.FromJulianDate(jd));
        Assert.AreEqual(DateTimeKind.Utc, VSOPTime.FromJulianDate(jd).Kind);

        VSOPTime time = new(tdb, TimeFrame.TDB);
        Assert.AreEqual(jd, time.JulianDate);
        Assert.AreEqual(jd - 2451545.0, time.J2000);
    }

    [TestMethod]
    public void VSOP87B_MatchesVSOP87AConvertedToSpherical()
    {
        CompareVersions(VSOPVersion.VSOP87A, VSOPVersion.VSOP87B, r => r.ToLBR(), 1e-6);
    }

    [TestMethod]
    public void VSOP87D_MatchesVSOP87CConvertedToSpherical()
    {
        CompareVersions(VSOPVersion.VSOP87C, VSOPVersion.VSOP87D, r => r.ToLBR(), 1e-6);
    }

    [TestMethod]
    public void VSOP87E_MatchesVSOP87AShiftedToBarycenter()
    {
        CompareVersions(VSOPVersion.VSOP87A, VSOPVersion.VSOP87E, r => r.ChangeFrame(FrameType.Barycentric, _calculator), 1e-6);
    }

    [TestMethod]
    public void VSOP87C_MatchesVSOP87ARotatedToDate()
    {
        // VSOP87C uses its own precession; IAU 1976 differs by ~0.05" after 500 years (~6e-6 au at Neptune).
        CompareVersions(VSOPVersion.VSOP87A, VSOPVersion.VSOP87C, r => r.ChangeEpoch(Epoch.OfDate), 1e-5);
    }

    [TestMethod]
    public void VSOP87B_MatchesVSOP87DRotatedToJ2000()
    {
        CompareVersions(VSOPVersion.VSOP87D, VSOPVersion.VSOP87B, r => r.ChangeEpoch(Epoch.J2000), 1e-6);
    }

    [TestMethod]
    public void VSOP87A_MatchesVSOP87EllipticConvertedToRectangular()
    {
        // Bounded by the VSOP87 precision p0 * a0 (Neptune: 42e-8 * 30.1 au ~ 1.3e-5 au).
        // The reverse direction is ill-conditioned: a depends on v^2, so 1e-5 relative velocity noise gives ~6e-4 au.
        CompareVersions(VSOPVersion.VSOP87, VSOPVersion.VSOP87A, r => r.ToXYZ(), 1e-5);
    }

    /// <summary>
    /// The VSOP87 versions are independent series fits of the same theory, so they agree only to the series precision.
    /// </summary>
    private void CompareVersions(VSOPVersion source, VSOPVersion target, Func<VSOPResult, VSOPResult> convert, double tolerance)
    {
        double max = 0d;
        string worst = string.Empty;
        double[] maxPerVariable = new double[6];
        VSOPBody[] bodies = Utility.ListAvailableBody(source).Intersect(Utility.ListAvailableBody(target)).ToArray();
        Assert.IsNotEmpty(bodies);
        foreach (VSOPBody body in bodies)
        {
            foreach (int year in new[] { 1500, 1900, 2000, 2024, 2500 })
            {
                VSOPTime time = new(new DateTime(year, 3, 1, 0, 0, 0, DateTimeKind.Utc), TimeFrame.TDB);
                VSOPResult expected = _calculator.GetPlanetPosition(body, target, time);
                VSOPResult actual = convert(_calculator.GetPlanetPosition(body, source, time));
                Assert.AreEqual(expected.CoordinatesType, actual.CoordinatesType);
                Assert.AreEqual(expected.FrameType, actual.FrameType);
                Assert.AreEqual(expected.Epoch, actual.Epoch);

                for (int i = 0; i < 6; i++)
                {
                    double diff = TestData.Diff(target, i, expected.Variables[i], actual.Variables[i]);
                    maxPerVariable[i] = Math.Max(maxPerVariable[i], diff);
                    if (diff > max)
                    {
                        max = diff;
                        worst = $"{body} {year} {TestData.Labels(target)[i]}";
                    }
                }
            }
        }
        string summary = $"{source} -> {target}: max diff {max:E3} at {worst}; per variable "
            + string.Join(", ", maxPerVariable.Select((d, i) => $"{TestData.Labels(target)[i]} {d:E2}"));
        TestContext.WriteLine(summary);

        Assert.IsTrue(max <= tolerance, $"{summary}; exceeds {tolerance:E0}.");
    }

    private static double NormalizeAngle(double angle)
    {
        return (angle % Math.Tau + Math.Tau) % Math.Tau;
    }

    private static void AssertComponents(double[] expected, double[] actual, double tolerance)
    {
        Assert.HasCount(expected.Length, actual);
        for (int index = 0; index < expected.Length; index++)
        {
            AssertClose(expected[index], actual[index], tolerance);
        }
    }

    private static void AssertClose(double expected, double actual, double tolerance)
    {
        Assert.IsTrue(
            Math.Abs(actual - expected) <= tolerance,
            $"Expected {expected:R}, actual {actual:R}, tolerance {tolerance:R}.");
    }
}
