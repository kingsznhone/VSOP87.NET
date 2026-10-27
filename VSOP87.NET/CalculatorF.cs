using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace VSOP87
{
    /// <summary>
    /// Fast, low precision approximation of VSOP87 intended for visualization, animation and teaching.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The periodic terms (time, coefficients, phase, sin/cos, accumulation) run in single precision over the full series.
    /// The few secular (polynomial) terms are evaluated in double, with the mean motion reduced modulo 2pi,
    /// so longitudes do not lose precision as they grow with time.
    /// </para>
    /// <para>
    /// Typical error over +-1000 years around J2000 (see the precision reports of the test project):
    /// elliptic elements (VSOP87) a up to ~6e-6 au, l/k/h ~2e-7, q/p ~1e-9;
    /// spherical longitudes (VSOP87B/D) up to ~1e-3 rad (~210 arcsec, Mercury);
    /// rectangular coordinates (VSOP87A/C/E) up to ~1e-3 au (Mercury); rates up to ~7e-5 au/day.
    /// Within +-100 years, longitudes stay below ~7e-5 rad (~14 arcsec) and coordinates below ~8e-5 au.
    /// The error is dominated by the float rounding of the phase B + C·t of the large periodic terms.
    /// </para>
    /// <para>
    /// Do not use it for precise astronomy (telescope pointing, eclipse or transit prediction);
    /// use <see cref="Calculator"/> when accuracy matters.
    /// </para>
    /// <para>Instances are read-only after construction and safe to share across threads.</para>
    /// </remarks>
    public class CalculatorF
    {
        /// <summary>
        /// One power table stored as SoA float arrays, zero padded to the selected vector width.
        /// </summary>
        private sealed class PowerSoA(float[] a, float[] b, float[] c)
        {
            /// <summary>Amplitude.</summary>
            public readonly float[] A = a;

            /// <summary>Phase.</summary>
            public readonly float[] B = b;

            /// <summary>Frequency.</summary>
            public readonly float[] C = c;
        }

        // [version * BodyCount + body] -> [variable]. Null when the body is not in the version.
        private readonly VariableSoA[]?[] _tables;

        /// <summary>
        /// One variable: periodic power tables (C != 0, float) and the secular polynomial (C = 0 terms, double).
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
        /// Creates a single precision calculator with the widest accelerated vector width.
        /// </summary>
        public CalculatorF() : this(SimdMode.Auto)
        {
        }

        /// <summary>
        /// Creates a single precision calculator using the specified vector width. The full series is always evaluated.
        /// </summary>
        /// <param name="mode">Vector width; <see cref="SimdMode.Auto"/> picks the widest accelerated width.</param>
        /// <exception cref="PlatformNotSupportedException">The requested width is not hardware accelerated.</exception>
        public CalculatorF(SimdMode mode)
        {
            Mode = VSOPData.ResolveMode(mode);
            _lanes = Mode switch
            {
                SimdMode.Vector512 => Vector512<float>.Count,
                SimdMode.Vector256 => Vector256<float>.Count,
                SimdMode.Vector128 => Vector128<float>.Count,
                _ => 1,
            };

            // Term[] is only needed while building the float SoA; it is dropped afterwards.
            _tables = new VariableSoA[]?[VSOPData.VersionCount * VSOPData.BodyCount];
            foreach (PlanetTable planet in VSOPData.Load())
            {
                int variableCount = planet.Version == VSOPVersion.VSOP87 ? 6 : 3;
                VariableSoA[] variables = new VariableSoA[variableCount];
                for (int iv = 0; iv < variableCount; iv++)
                {
                    PowerTable[] powers = planet.variables.TryGetValue((VSOPVariable)(iv + 1), out VariableTable? variable) && variable.PowerTables != null
                        ? variable.PowerTables
                        : [];
                    double[] secular = new double[powers.Length];
                    double[] secularLo = new double[powers.Length];
                    PowerSoA?[] periodic = new PowerSoA?[powers.Length];
                    for (int it = 0; it < powers.Length; it++)
                    {
                        periodic[it] = BuildSoA(powers[it], it, secular, secularLo);
                    }
                    variables[iv] = new VariableSoA(periodic, secular, secularLo);
                }
                _tables[Index(planet.Version, planet.Body)] = variables;
            }
        }

        private static int Index(VSOPVersion version, VSOPBody body) => (int)version * VSOPData.BodyCount + (int)body;

        private PowerSoA? BuildSoA(PowerTable? power, int it, double[] secular, double[] secularLo)
        {
            if (power?.Terms is not { Length: > 0 } source)
            {
                return null;
            }

            Term[] terms = VSOPData.SplitSecular(source, it, secular, secularLo);
            if (terms.Length == 0)
            {
                return null;
            }

            int len = terms.Length;
            // Zero padding (A = 0) contributes exactly 0, so the SIMD loops need no scalar tail.
            int padded = (len + _lanes - 1) / _lanes * _lanes;
            float[] a = new float[padded];
            float[] b = new float[padded];
            float[] c = new float[padded];
            for (int i = 0; i < len; i++)
            {
                a[i] = (float)terms[i].A;
                b[i] = (float)terms[i].B;
                c[i] = (float)terms[i].C;
            }
            return new PowerSoA(a, b, c);
        }

        /// <summary>
        /// Calculates the position of a body in single precision.
        /// </summary>
        /// <param name="ibody">VSOP87 Planet</param>
        /// <param name="iver">VSOP87 Version</param>
        /// <param name="time">Time of evaluation</param>
        /// <returns>Result contain version, body, coordinates reference/type, time frame, and variables (widened from float)</returns>
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
        /// <returns>Result contain version, body, coordinates reference/type, time frame, and variables (widened from float)</returns>
        /// <exception cref="ArgumentException">The body is not available in the version.</exception>
        public Task<VSOPResult> GetPlanetPositionAsync(VSOPBody ibody, VSOPVersion iver, VSOPTime time)
        {
            return Task.Run(() => GetPlanetPosition(ibody, iver, time));
        }

        /// <summary>
        /// Periodic part in float; the secular polynomial (including the mean motion reduced modulo 2pi) in double.
        /// </summary>
        private double[] Calculate(VariableSoA[] variables, VSOPVersion version, VSOPTime time)
        {
            double td = VSOPData.Millennia(time, out double tError);
            float t = (float)td;
            bool withRate = version != VSOPVersion.VSOP87;
            int angle = VSOPData.AngleIndex(version);
            double[] r = new double[6];
            for (int iv = 0; iv < variables.Length; iv++)
            {
                PowerSoA?[] powers = variables[iv].Powers;
                float value = 0f;
                float rate = 0f;
                // tPrev = t^(it-1), tPow = t^it
                float tPrev = 0f;
                float tPow = 1f;
                for (int it = 0; it < powers.Length; it++, tPrev = tPow, tPow *= t)
                {
                    PowerSoA? power = powers[it];
                    if (power == null)
                    {
                        continue;
                    }

                    (float sumCos, float sumSin) = Mode switch
                    {
                        SimdMode.Vector512 => SumV512(power, t, withRate),
                        SimdMode.Vector256 => SumV256(power, t, withRate),
                        SimdMode.Vector128 => SumV128(power, t, withRate),
                        _ => SumScalar(power, t, withRate),
                    };
                    value = MathF.FusedMultiplyAdd(tPow, sumCos, value);
                    rate += it * tPrev * sumCos - tPow * sumSin;
                }

                (double secularValue, double secularRate) = VSOPData.Secular(variables[iv].Secular, variables[iv].SecularLo, td, tError, iv == angle);
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static (float Cos, float Sin) SumScalar(PowerSoA power, float t, bool withRate)
        {
            float[] a = power.A;
            ReadOnlySpan<float> b = power.B.AsSpan(0, a.Length);
            ReadOnlySpan<float> c = power.C.AsSpan(0, a.Length);
            float sumCos = 0f;
            float sumSin = 0f;
            if (withRate)
            {
                for (int n = 0; n < a.Length; n++)
                {
                    (float su, float cu) = MathF.SinCos(MathF.FusedMultiplyAdd(c[n], t, b[n]));
                    sumCos = MathF.FusedMultiplyAdd(a[n], cu, sumCos);
                    sumSin = MathF.FusedMultiplyAdd(a[n] * c[n], su, sumSin);
                }
            }
            else
            {
                for (int n = 0; n < a.Length; n++)
                {
                    sumCos = MathF.FusedMultiplyAdd(a[n], MathF.Cos(MathF.FusedMultiplyAdd(c[n], t, b[n])), sumCos);
                }
            }
            return (sumCos, sumSin);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static (float Cos, float Sin) SumV512(PowerSoA power, float t, bool withRate)
        {
            int len = power.A.Length;
            ref float a = ref MemoryMarshal.GetArrayDataReference(power.A);
            ref float b = ref MemoryMarshal.GetArrayDataReference(power.B);
            ref float c = ref MemoryMarshal.GetArrayDataReference(power.C);
            Vector512<float> vt = Vector512.Create(t);
            Vector512<float> accCos = Vector512<float>.Zero;
            Vector512<float> accSin = Vector512<float>.Zero;
            if (withRate)
            {
                for (int n = 0; n < len; n += Vector512<float>.Count)
                {
                    Vector512<float> va = Vector512.LoadUnsafe(ref a, (nuint)n);
                    Vector512<float> vc = Vector512.LoadUnsafe(ref c, (nuint)n);
                    (Vector512<float> su, Vector512<float> cu) = Vector512.SinCos(Vector512.FusedMultiplyAdd(vc, vt, Vector512.LoadUnsafe(ref b, (nuint)n)));
                    accCos = Vector512.FusedMultiplyAdd(va, cu, accCos);
                    accSin = Vector512.FusedMultiplyAdd(va * vc, su, accSin);
                }
            }
            else
            {
                for (int n = 0; n < len; n += Vector512<float>.Count)
                {
                    Vector512<float> u = Vector512.FusedMultiplyAdd(Vector512.LoadUnsafe(ref c, (nuint)n), vt, Vector512.LoadUnsafe(ref b, (nuint)n));
                    accCos = Vector512.FusedMultiplyAdd(Vector512.LoadUnsafe(ref a, (nuint)n), Vector512.Cos(u), accCos);
                }
            }
            return (Vector512.Sum(accCos), Vector512.Sum(accSin));
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static (float Cos, float Sin) SumV256(PowerSoA power, float t, bool withRate)
        {
            int len = power.A.Length;
            ref float a = ref MemoryMarshal.GetArrayDataReference(power.A);
            ref float b = ref MemoryMarshal.GetArrayDataReference(power.B);
            ref float c = ref MemoryMarshal.GetArrayDataReference(power.C);
            Vector256<float> vt = Vector256.Create(t);
            Vector256<float> accCos = Vector256<float>.Zero;
            Vector256<float> accSin = Vector256<float>.Zero;
            if (withRate)
            {
                for (int n = 0; n < len; n += Vector256<float>.Count)
                {
                    Vector256<float> va = Vector256.LoadUnsafe(ref a, (nuint)n);
                    Vector256<float> vc = Vector256.LoadUnsafe(ref c, (nuint)n);
                    (Vector256<float> su, Vector256<float> cu) = Vector256.SinCos(Vector256.FusedMultiplyAdd(vc, vt, Vector256.LoadUnsafe(ref b, (nuint)n)));
                    accCos = Vector256.FusedMultiplyAdd(va, cu, accCos);
                    accSin = Vector256.FusedMultiplyAdd(va * vc, su, accSin);
                }
            }
            else
            {
                for (int n = 0; n < len; n += Vector256<float>.Count)
                {
                    Vector256<float> u = Vector256.FusedMultiplyAdd(Vector256.LoadUnsafe(ref c, (nuint)n), vt, Vector256.LoadUnsafe(ref b, (nuint)n));
                    accCos = Vector256.FusedMultiplyAdd(Vector256.LoadUnsafe(ref a, (nuint)n), Vector256.Cos(u), accCos);
                }
            }
            return (Vector256.Sum(accCos), Vector256.Sum(accSin));
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static (float Cos, float Sin) SumV128(PowerSoA power, float t, bool withRate)
        {
            int len = power.A.Length;
            ref float a = ref MemoryMarshal.GetArrayDataReference(power.A);
            ref float b = ref MemoryMarshal.GetArrayDataReference(power.B);
            ref float c = ref MemoryMarshal.GetArrayDataReference(power.C);
            Vector128<float> vt = Vector128.Create(t);
            Vector128<float> accCos = Vector128<float>.Zero;
            Vector128<float> accSin = Vector128<float>.Zero;
            if (withRate)
            {
                for (int n = 0; n < len; n += Vector128<float>.Count)
                {
                    Vector128<float> va = Vector128.LoadUnsafe(ref a, (nuint)n);
                    Vector128<float> vc = Vector128.LoadUnsafe(ref c, (nuint)n);
                    (Vector128<float> su, Vector128<float> cu) = Vector128.SinCos(Vector128.FusedMultiplyAdd(vc, vt, Vector128.LoadUnsafe(ref b, (nuint)n)));
                    accCos = Vector128.FusedMultiplyAdd(va, cu, accCos);
                    accSin = Vector128.FusedMultiplyAdd(va * vc, su, accSin);
                }
            }
            else
            {
                for (int n = 0; n < len; n += Vector128<float>.Count)
                {
                    Vector128<float> u = Vector128.FusedMultiplyAdd(Vector128.LoadUnsafe(ref c, (nuint)n), vt, Vector128.LoadUnsafe(ref b, (nuint)n));
                    accCos = Vector128.FusedMultiplyAdd(Vector128.LoadUnsafe(ref a, (nuint)n), Vector128.Cos(u), accCos);
                }
            }
            return (Vector128.Sum(accCos), Vector128.Sum(accSin));
        }
    }
}
