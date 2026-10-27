using VSOP87;

// Reference values: official VSOP87.chk (TDB).
const double Tolerance = 1e-9;
const double FloatTolerance = 1e-2;

(VSOPVersion Version, VSOPBody Body, double JulianDate, double[] Expected)[] cases =
[
    // a, l, k, h, q, p
    (VSOPVersion.VSOP87, VSOPBody.MERCURY, 2451545.0, [0.3870982122, 4.4026057779, 0.0446647518, 0.2007208958, 0.0406161541, 0.0456355031]),
    // l, b, r, l', b', r'
    (VSOPVersion.VSOP87D, VSOPBody.EARTH, 2451545.0, [1.7519238681, -0.0000039656, 0.9833276819, 0.0177924465, 0.0000001146, -0.0000073533]),
    // x, y, z, x', y', z'
    (VSOPVersion.VSOP87A, VSOPBody.JUPITER, 2378495.0, [-0.0180390004, 5.1317748839, -0.0200448490, -0.0076399285, 0.0003252123, 0.0001704041]),
];

Calculator calculator = new();
CalculatorF fast = new();
Console.WriteLine($"Calculator mode: {calculator.Mode}, CalculatorF mode: {fast.Mode}");

int failures = 0;
foreach ((VSOPVersion version, VSOPBody body, double jd, double[] expected) in cases)
{
    VSOPTime time = new(new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddDays(jd - 2451545.0), TimeFrame.TDB);
    int angle = version == VSOPVersion.VSOP87 ? 1 : version is VSOPVersion.VSOP87B or VSOPVersion.VSOP87D ? 0 : -1;
    failures += Check($"{version} {body}", calculator.GetPlanetPosition(body, version, time).Variables.ToArray(), expected, angle, Tolerance);
    failures += Check($"{version} {body} (CalculatorF)", fast.GetPlanetPosition(body, version, time).Variables.ToArray(), expected, angle, FloatTolerance);
}

Console.WriteLine(failures == 0 ? "Smoke test passed." : "Smoke test FAILED.");
return failures == 0 ? 0 : 1;

static int Check(string name, double[] actual, double[] expected, int angleIndex, double tolerance)
{
    double max = 0d;
    for (int i = 0; i < expected.Length; i++)
    {
        double diff = Math.Abs(actual[i] - expected[i]);
        if (i == angleIndex)
        {
            diff %= Math.Tau;
            diff = Math.Min(diff, Math.Tau - diff);
        }
        max = Math.Max(max, diff);
    }
    bool ok = max <= tolerance;
    Console.WriteLine($"{name}: max error {max:E3} (tolerance {tolerance:E0}) {(ok ? "OK" : "FAIL")}");
    return ok ? 0 : 1;
}
