using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace VSOP87
{
    /// <summary>
    /// Selects the vector width used by <see cref="Calculator"/> to evaluate the series.
    /// </summary>
    public enum SimdMode
    {
        /// <summary>Pick the widest hardware-accelerated width available.</summary>
        Auto = 0,

        /// <summary>Scalar evaluation only.</summary>
        Scalar = 1,

        /// <summary>128-bit vectors (2 doubles).</summary>
        Vector128 = 2,

        /// <summary>256-bit vectors (4 doubles).</summary>
        Vector256 = 3,

        /// <summary>512-bit vectors (8 doubles).</summary>
        Vector512 = 4,
    }

    /// <summary>
    /// Selects how much of the series <see cref="Calculator"/> evaluates.
    /// </summary>
    /// <remarks>
    /// Truncated modes drop the periodic terms with the smallest worst-case contribution, as long as the sum of the
    /// dropped contributions stays within the budget, so the error added by truncation is guaranteed for |t - J2000| &lt;= 1000 years.
    /// The budget follows the VSOP87 precision convention: the tolerance in radians for angles and k/h/q/p,
    /// the tolerance times the semi-major axis a0 in au for distances and rectangular coordinates,
    /// and the same values per day for the rates.
    /// </remarks>
    public enum PrecisionMode
    {
        /// <summary>Full series; reproduces the official VSOP87.chk values.</summary>
        Full = 0,

        /// <summary>Truncation error at most 1 arcsecond (about the precision of the VSOP87 theory itself).</summary>
        Fast = 1,

        /// <summary>Truncation error at most 10 arcseconds, for visualization.</summary>
        UltraFast = 2,
    }

    /// <summary>
    /// VSOP87 calculator (double precision). <see cref="PrecisionMode.Full"/> reproduces the official VSOP87.chk values;
    /// <see cref="PrecisionMode.Fast"/> and <see cref="PrecisionMode.UltraFast"/> drop terms with a guaranteed error bound.
    /// </summary>
    /// <remarks>
    /// Instances are read-only after construction and safe to share across threads.
    /// </remarks>
    public class Calculator
    {
        /// <summary>
        /// One power table stored as SoA arrays, zero padded to the selected vector width.
        /// </summary>
        private sealed class PowerSoA(double[] a, double[] b, double[] c)
        {
            /// <summary>Amplitude.</summary>
            public readonly double[] A = a;

            /// <summary>Phase.</summary>
            public readonly double[] B = b;

            /// <summary>Frequency.</summary>
            public readonly double[] C = c;
        }

        // [version * BodyCount + body] -> [variable]. Null when the body is not in the version.
        private readonly VariableSoA[]?[] _tables;

        /// <summary>
        /// One variable: periodic power tables (C != 0) and the secular polynomial (C = 0 terms).
        /// </summary>
        private sealed class VariableSoA(PowerSoA?[] powers, double[] secular, double[] secularLo)
        {
            /// <summary>Periodic terms per power of t; null entries are empty.</summary>
            public readonly PowerSoA?[] Powers = powers;

            /// <summary>Secular coefficient per power of t: sum of A·cos(B) over the C = 0 terms (high part).</summary>
            public readonly double[] Secular = secular;

            /// <summary>Rounding error of <see cref="Secular"/>, so that Secular + SecularLo is the exact sum.</summary>
            public readonly double[] SecularLo = secularLo;
        }

        private readonly int _lanes;

        /// <summary>
        /// Vector width actually used by this instance after hardware detection.
        /// </summary>
        public SimdMode Mode { get; }

        /// <summary>
        /// How much of the series is evaluated.
        /// </summary>
        public PrecisionMode Precision { get; }

        /// <summary>
        /// Number of periodic terms kept (without SIMD padding), for diagnostics.
        /// </summary>
        internal int PeriodicTermCount { get; }

        /// <summary>
        /// Creates a full precision calculator with the widest accelerated vector width.
        /// </summary>
        public Calculator() : this(SimdMode.Auto)
        {
        }

        /// <summary>
        /// Creates a calculator using the specified vector width and precision.
        /// </summary>
        /// <param name="mode">Vector width; <see cref="SimdMode.Auto"/> picks the widest accelerated width.</param>
        /// <param name="precision">
        /// <see cref="PrecisionMode.Full"/> (default) evaluates the full series. <see cref="PrecisionMode.Fast"/> (1")
        /// and <see cref="PrecisionMode.UltraFast"/> (10") drop terms while bounding the truncation error for |t - J2000| &lt;= 1000 years.
        /// </param>
        /// <exception cref="PlatformNotSupportedException">The requested width is not hardware accelerated.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="precision"/> is not a defined value.</exception>
        public Calculator(SimdMode mode, PrecisionMode precision = PrecisionMode.Full)
        {
            Mode = VSOPData.ResolveMode(mode);
            Precision = precision;
            double tolerance = VSOPData.ToleranceArcsec(precision);
            _lanes = Mode switch
            {
                SimdMode.Vector512 => Vector512<double>.Count,
                SimdMode.Vector256 => Vector256<double>.Count,
                SimdMode.Vector128 => Vector128<double>.Count,
                _ => 1,
            };

            // Term[] is only needed while building the SoA; it is dropped afterwards.
            _tables = new VariableSoA[]?[VSOPData.VersionCount * VSOPData.BodyCount];
            int termCount = 0;
            foreach (PlanetTable planet in VSOPData.Load())
            {
                int variableCount = planet.Version == VSOPVersion.VSOP87 ? 6 : 3;
                bool withRate = planet.Version != VSOPVersion.VSOP87;
                VariableSoA[] variables = new VariableSoA[variableCount];
                for (int iv = 0; iv < variableCount; iv++)
                {
                    PowerTable[] powers = planet.variables.TryGetValue((VSOPVariable)(iv + 1), out VariableTable? variable) && variable.PowerTables != null
                        ? variable.PowerTables
                        : [];
                    double[] secular = new double[powers.Length];
                    double[] secularLo = new double[powers.Length];
                    Term[][] periodic = new Term[powers.Length][];
                    for (int it = 0; it < powers.Length; it++)
                    {
                        periodic[it] = VSOPData.SplitSecular(powers[it]?.Terms ?? [], it, secular, secularLo);
                    }

                    double budget = VSOPData.Budget(planet.Version, planet.Body, iv, tolerance);
                    periodic = VSOPData.Truncate(periodic, budget, withRate);
                    termCount += periodic.Sum(p => p.Length);
                    variables[iv] = new VariableSoA(Array.ConvertAll(periodic, BuildSoA), secular, secularLo);
                }
                _tables[Index(planet.Version, planet.Body)] = variables;
            }
            PeriodicTermCount = termCount;
        }

        private static int Index(VSOPVersion version, VSOPBody body) => (int)version * VSOPData.BodyCount + (int)body;

        private PowerSoA? BuildSoA(Term[] terms)
        {
            if (terms.Length == 0)
            {
                return null;
            }

            int len = terms.Length;
            // Zero padding (A = 0) contributes exactly 0, so the SIMD loops need no scalar tail.
            int padded = (len + _lanes - 1) / _lanes * _lanes;
            double[] a = new double[padded];
            double[] b = new double[padded];
            double[] c = new double[padded];
            for (int i = 0; i < len; i++)
            {
                a[i] = terms[i].A;
                b[i] = terms[i].B;
                c[i] = terms[i].C;
            }
            return new PowerSoA(a, b, c);
        }

        /// <summary>
        /// Calculates the position of a body.
        /// </summary>
        /// <param name="ibody">VSOP87 Planet</param>
        /// <param name="iver">VSOP87 Version</param>
        /// <param name="time">Time of evaluation</param>
        /// <returns>Result contain version, body, coordinates reference/type, time frame,and variables</returns>
        /// <exception cref="ArgumentException">The body is not available in the version.</exception>
        public VSOPResult GetPlanetPosition(VSOPBody ibody, VSOPVersion iver, VSOPTime time)
        {
            if (!Utility.CheckAvailability(iver, ibody) || _tables[Index(iver, ibody)] is not { } variables)
            {
                throw new ArgumentException("No body in this version.");
            }

            return VSOPData.CreateResult(ibody, iver, time, Calculate(variables, iver, time));
        }

        /// <summary>
        /// Runs <see cref="GetPlanetPosition"/> on the thread pool.
        /// </summary>
        /// <param name="ibody">VSOP87 Planet</param>
        /// <param name="iver">VSOP87 Version</param>
        /// <param name="time">Time of evaluation</param>
        /// <returns>Result contain version, body, coordinates reference/type, time frame,and variables</returns>
        /// <exception cref="ArgumentException">The body is not available in the version.</exception>
        public Task<VSOPResult> GetPlanetPositionAsync(VSOPBody ibody, VSOPVersion iver, VSOPTime time)
        {
            return Task.Run(() => GetPlanetPosition(ibody, iver, time));
        }

        /// <summary>
        /// Evaluates every variable: value = sum t^a * A cos(B + C t); rate = d(value)/dt per day (not for VSOP87 elliptic).
        /// The periodic part and the secular polynomial are summed separately so the accumulator stays small;
        /// the mean motion of the angular variable is reduced modulo 2pi before being added.
        /// </summary>
        private double[] Calculate(VariableSoA[] variables, VSOPVersion version, VSOPTime time)
        {
            double t = VSOPData.Millennia(time, out double tError);
            bool withRate = version != VSOPVersion.VSOP87;
            int angle = VSOPData.AngleIndex(version);
            double[] r = new double[6];
            for (int iv = 0; iv < variables.Length; iv++)
            {
                PowerSoA?[] powers = variables[iv].Powers;
                double value = 0d;
                double rate = 0d;
                // tPrev = t^(it-1), tPow = t^it
                double tPrev = 0d;
                double tPow = 1d;
                for (int it = 0; it < powers.Length; it++, tPrev = tPow, tPow *= t)
                {
                    PowerSoA? power = powers[it];
                    if (power == null)
                    {
                        continue;
                    }

                    (double sumCos, double sumSin) = Mode switch
                    {
                        SimdMode.Vector512 => SumV512(power, t, withRate),
                        SimdMode.Vector256 => SumV256(power, t, withRate),
                        SimdMode.Vector128 => SumV128(power, t, withRate),
                        _ => SumScalar(power, t, withRate),
                    };
                    value = Math.FusedMultiplyAdd(tPow, sumCos, value);
                    rate += it * tPrev * sumCos - tPow * sumSin;
                }

                (double secularValue, double secularRate) = VSOPData.Secular(variables[iv].Secular, variables[iv].SecularLo, t, tError, iv == angle);
                r[iv] = value + secularValue;
                if (withRate)
                {
                    r[iv + 3] = (rate + secularRate) / VSOPData.A1000;
                }
            }

            if (angle >= 0)
            {
                r[angle] = VSOPData.NormalizeAngle(r[angle]);
            }
            return r;
        }

        /// <summary>
        /// Returns (sum A cos(u), sum A C sin(u)) with u = B + C t; the second sum is 0 when <paramref name="withRate"/> is false.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static (double Cos, double Sin) SumScalar(PowerSoA power, double t, bool withRate)
        {
            double[] a = power.A;
            ReadOnlySpan<double> b = power.B.AsSpan(0, a.Length);
            ReadOnlySpan<double> c = power.C.AsSpan(0, a.Length);
            double sumCos = 0d;
            double sumSin = 0d;
            if (withRate)
            {
                for (int n = 0; n < a.Length; n++)
                {
                    (double su, double cu) = Math.SinCos(Math.FusedMultiplyAdd(c[n], t, b[n]));
                    sumCos = Math.FusedMultiplyAdd(a[n], cu, sumCos);
                    sumSin = Math.FusedMultiplyAdd(a[n] * c[n], su, sumSin);
                }
            }
            else
            {
                for (int n = 0; n < a.Length; n++)
                {
                    sumCos = Math.FusedMultiplyAdd(a[n], Math.Cos(Math.FusedMultiplyAdd(c[n], t, b[n])), sumCos);
                }
            }
            return (sumCos, sumSin);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static (double Cos, double Sin) SumV512(PowerSoA power, double t, bool withRate)
        {
            int len = power.A.Length;
            ref double a = ref MemoryMarshal.GetArrayDataReference(power.A);
            ref double b = ref MemoryMarshal.GetArrayDataReference(power.B);
            ref double c = ref MemoryMarshal.GetArrayDataReference(power.C);
            Vector512<double> vt = Vector512.Create(t);
            Vector512<double> accCos = Vector512<double>.Zero;
            Vector512<double> accSin = Vector512<double>.Zero;
            if (withRate)
            {
                for (int n = 0; n < len; n += Vector512<double>.Count)
                {
                    Vector512<double> va = Vector512.LoadUnsafe(ref a, (nuint)n);
                    Vector512<double> vc = Vector512.LoadUnsafe(ref c, (nuint)n);
                    (Vector512<double> su, Vector512<double> cu) = Vector512.SinCos(Vector512.FusedMultiplyAdd(vc, vt, Vector512.LoadUnsafe(ref b, (nuint)n)));
                    accCos = Vector512.FusedMultiplyAdd(va, cu, accCos);
                    accSin = Vector512.FusedMultiplyAdd(va * vc, su, accSin);
                }
            }
            else
            {
                for (int n = 0; n < len; n += Vector512<double>.Count)
                {
                    Vector512<double> u = Vector512.FusedMultiplyAdd(Vector512.LoadUnsafe(ref c, (nuint)n), vt, Vector512.LoadUnsafe(ref b, (nuint)n));
                    accCos = Vector512.FusedMultiplyAdd(Vector512.LoadUnsafe(ref a, (nuint)n), Vector512.Cos(u), accCos);
                }
            }
            return (Vector512.Sum(accCos), Vector512.Sum(accSin));
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static (double Cos, double Sin) SumV256(PowerSoA power, double t, bool withRate)
        {
            int len = power.A.Length;
            ref double a = ref MemoryMarshal.GetArrayDataReference(power.A);
            ref double b = ref MemoryMarshal.GetArrayDataReference(power.B);
            ref double c = ref MemoryMarshal.GetArrayDataReference(power.C);
            Vector256<double> vt = Vector256.Create(t);
            Vector256<double> accCos = Vector256<double>.Zero;
            Vector256<double> accSin = Vector256<double>.Zero;
            if (withRate)
            {
                for (int n = 0; n < len; n += Vector256<double>.Count)
                {
                    Vector256<double> va = Vector256.LoadUnsafe(ref a, (nuint)n);
                    Vector256<double> vc = Vector256.LoadUnsafe(ref c, (nuint)n);
                    (Vector256<double> su, Vector256<double> cu) = Vector256.SinCos(Vector256.FusedMultiplyAdd(vc, vt, Vector256.LoadUnsafe(ref b, (nuint)n)));
                    accCos = Vector256.FusedMultiplyAdd(va, cu, accCos);
                    accSin = Vector256.FusedMultiplyAdd(va * vc, su, accSin);
                }
            }
            else
            {
                for (int n = 0; n < len; n += Vector256<double>.Count)
                {
                    Vector256<double> u = Vector256.FusedMultiplyAdd(Vector256.LoadUnsafe(ref c, (nuint)n), vt, Vector256.LoadUnsafe(ref b, (nuint)n));
                    accCos = Vector256.FusedMultiplyAdd(Vector256.LoadUnsafe(ref a, (nuint)n), Vector256.Cos(u), accCos);
                }
            }
            return (Vector256.Sum(accCos), Vector256.Sum(accSin));
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static (double Cos, double Sin) SumV128(PowerSoA power, double t, bool withRate)
        {
            int len = power.A.Length;
            ref double a = ref MemoryMarshal.GetArrayDataReference(power.A);
            ref double b = ref MemoryMarshal.GetArrayDataReference(power.B);
            ref double c = ref MemoryMarshal.GetArrayDataReference(power.C);
            Vector128<double> vt = Vector128.Create(t);
            Vector128<double> accCos = Vector128<double>.Zero;
            Vector128<double> accSin = Vector128<double>.Zero;
            if (withRate)
            {
                for (int n = 0; n < len; n += Vector128<double>.Count)
                {
                    Vector128<double> va = Vector128.LoadUnsafe(ref a, (nuint)n);
                    Vector128<double> vc = Vector128.LoadUnsafe(ref c, (nuint)n);
                    (Vector128<double> su, Vector128<double> cu) = Vector128.SinCos(Vector128.FusedMultiplyAdd(vc, vt, Vector128.LoadUnsafe(ref b, (nuint)n)));
                    accCos = Vector128.FusedMultiplyAdd(va, cu, accCos);
                    accSin = Vector128.FusedMultiplyAdd(va * vc, su, accSin);
                }
            }
            else
            {
                for (int n = 0; n < len; n += Vector128<double>.Count)
                {
                    Vector128<double> u = Vector128.FusedMultiplyAdd(Vector128.LoadUnsafe(ref c, (nuint)n), vt, Vector128.LoadUnsafe(ref b, (nuint)n));
                    accCos = Vector128.FusedMultiplyAdd(Vector128.LoadUnsafe(ref a, (nuint)n), Vector128.Cos(u), accCos);
                }
            }
            return (Vector128.Sum(accCos), Vector128.Sum(accSin));
        }
    }
}
