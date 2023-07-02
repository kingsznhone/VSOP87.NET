using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Serialization.Formatters.Binary;
using BenchmarkDotNet.Attributes;

namespace VSOP87
{
    [MemoryDiagnoser]
    public class CalculatorF
    {
        public readonly List<PlanetTableF> VSOP87DATAF;
        public readonly int vectorSize = Vector<float>.Count;

        private float[] array_u = new float[2048];
        private float[] array_cu = new float[2048];
        private float[] array_su = new float[2048];
        private float[] array_a_cu_tit = new float[2048];
        private float[] array_R = new float[2048];

        public CalculatorF()
        {
            using (Stream ms = Assembly.GetExecutingAssembly().GetManifestResourceStream("VSOP87.NET.Resources.VSOP87DATAF.BIN"))
            {
#pragma warning disable SYSLIB0011 // 类型或成员已过时
                VSOP87DATAF = (List<PlanetTableF>)new BinaryFormatter().Deserialize(ms);
#pragma warning restore SYSLIB0011 // 类型或成员已过时
                Console.WriteLine("VSOP87DATAF.BIN Loaded");
            }

            int termmax = 0;
            //Prepare SIMD Array
            for (int ip = 0; ip < VSOP87DATAF.Count; ip++)
            {
                for (int iv = 0; iv < 6; iv++)
                {
                    for (int it = 5; it >= 0; it--)
                    {
                        if (VSOP87DATAF[ip].variables[iv].PowerTables is null) continue;
                        if (VSOP87DATAF[ip].variables[iv].PowerTables[it].Terms is null) continue;

                        //Prepare SIMD Array
                        var terms = VSOP87DATAF[ip].variables[iv].PowerTables[it].Terms;
                        float[] buffer = new float[terms.Length];
                        Array.Copy(terms.Select(x => x.A).ToArray(), buffer, terms.Length);
                        VSOP87DATAF[ip].variables[iv].PowerTables[it].Array_A = buffer;

                        buffer = new float[terms.Length];
                        Array.Copy(terms.Select(x => x.B).ToArray(), buffer, terms.Length);
                        VSOP87DATAF[ip].variables[iv].PowerTables[it].Array_B = buffer;

                        buffer = new float[terms.Length];
                        Array.Copy(terms.Select(x => x.C).ToArray(), buffer, terms.Length);
                        VSOP87DATAF[ip].variables[iv].PowerTables[it].Array_C = buffer;
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ibody">VSOP87 Planet</param>
        /// <param name="iver">VSOP87 Version</param>
        /// <param name="TDB">Barycentric Dynamical Time</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public VSOPResultF GetPlanet(VSOPBody ibody, VSOPVersion iver, VSOPTime time)
        {
            if (Utility.CheckAvailability(iver, ibody))
            {
                int tableIndex = VSOP87DATAF.FindIndex(x => x.version == iver && x.body == ibody);

                float[] result = Calculate(VSOP87DATAF[tableIndex], (float)VSOPTime.ToJulianDate(time.TDB));

                switch (iver)
                {
                    case VSOPVersion.VSOP87:
                        return new VSOPResultFELL(iver, ibody, time, result);

                    case VSOPVersion.VSOP87A or VSOPVersion.VSOP87C or VSOPVersion.VSOP87E:
                        return new VSOPResultFXYZ(iver, ibody, time, result);

                    case VSOPVersion.VSOP87B or VSOPVersion.VSOP87D:
                        return new VSOPResultFLBR(iver, ibody, time, result);

                    default: throw new ArgumentException();
                }
            }
            else
            {
                throw new ArgumentException("No body in this version.");
            }
        }

        public VSOPResultF GetPlanet_SIMD(VSOPBody ibody, VSOPVersion iver, VSOPTime time)
        {
            if (Utility.CheckAvailability(iver, ibody))
            {
                int tableIndex = VSOP87DATAF.FindIndex(x => x.version == iver && x.body == ibody);

                float[] result_SIMD = Calculate_SIMD(VSOP87DATAF[tableIndex], (float)VSOPTime.ToJulianDate(time.TDB));
                switch (iver)
                {
                    case VSOPVersion.VSOP87:
                        return new VSOPResultFELL(iver, ibody, time, result_SIMD);

                    case VSOPVersion.VSOP87A or VSOPVersion.VSOP87C or VSOPVersion.VSOP87E:
                        return new VSOPResultFXYZ(iver, ibody, time, result_SIMD);

                    case VSOPVersion.VSOP87B or VSOPVersion.VSOP87D:
                        return new VSOPResultFLBR(iver, ibody, time, result_SIMD);

                    default: throw new ArgumentException();
                }
            }
            else
            {
                throw new ArgumentException("No body in this version.");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="Planet">Dataset of a planet</param>
        /// <param name="JD">Julian Date</param>
        /// <returns></returns>
        private float[] Calculate(PlanetTableF Planet, float JD)
        {
            float phi = (JD - 2451545.0f) / 365250f;

            Span<float> t = stackalloc float[6];
            for (int i = 0; i < 6; i++)
            {
                t[i] = MathF.Pow(phi, i);
            }

            Span<float> Result = stackalloc float[6];
            float u, cu, su;
            float tit;//t[it]
            for (int iv = 0; iv < 6; iv++)
            {
                for (int it = 5; it >= 0; it--)
                {
                    tit = t[it];
                    if (Planet.variables[iv].PowerTables is null) continue;
                    if (Planet.variables[iv].PowerTables[it].Terms is null) continue;
                    foreach (TermF term in Planet.variables[iv].PowerTables[it].Terms)
                    {
                        //u = b + c * phi
                        //su = Math.Sin(u);
                        //cu= Math.Cos(u);
                        (su, cu) = MathF.SinCos(term.B + term.C * phi);
                        Result[iv] = Result[iv] + term.A * cu * tit;

                        // Original resolution specification.
                        if (Planet.version == VSOPVersion.VSOP87) continue;

                        // Derivative of 3 variables
                        if (it == 0)
                            Result[iv + 3] += (0 * it * term.A * cu) - (tit * term.A * term.C * su);
                        else
                            Result[iv + 3] += (t[it - 1] * it * term.A * cu) - (tit * term.A * term.C * su);
                    }
                }
            }

            // Original resolution specification.
            if (Planet.version == VSOPVersion.VSOP87) return Result.ToArray();
            for (int ic = 0; ic < 3; ic++)
            {
                Result[ic + 3] /= 365250f;
            }

            //Modulo Spherical longitude L into [0,2*pi)
            if (Utility.GetCoordinatesType(Planet.version) == CoordinatesType.Spherical)
            {
                ModuloCircle(ref Result[0]);
            }

            return Result.ToArray();
        }

        private float[] Calculate_SIMD(PlanetTableF Planet, float JD)
        {
            float phi = (JD - 2451545.0f) / 365250f;

            Span<float> t = stackalloc float[6];
            for (int i = 0; i < 6; i++)
            {
                t[i] = MathF.Pow(phi, i);
            }

            Span<float> Result = stackalloc float[6];
            Vector256<float> sum = new Vector256<float>();
            float u, cu, su;
            float tit;// =t[it]

            Span<float> span_u = new Span<float>(array_u);
            Span<float> span_cu = new Span<float>(array_cu);
            Span<float> span_su = new Span<float>(array_su);
            Span<float> span_a_cu_tit = new Span<float>(array_a_cu_tit);
            Span<float> span_R = new Span<float>(array_R);

            ref float ref_u = ref MemoryMarshal.GetReference<float>(array_u);
            ref float ref_cu = ref MemoryMarshal.GetReference<float>(array_cu);
            ref float ref_su = ref MemoryMarshal.GetReference<float>(array_su);
            ref float ref_a_cu_tit = ref MemoryMarshal.GetReference<float>(array_a_cu_tit);
            ref float ref_R = ref MemoryMarshal.GetReference<float>(array_R);

            for (int iv = 0; iv < 6; iv++)
            {
                for (int it = 5; it >= 0; it--)
                {
                    tit = t[it];
                    if (Planet.variables[iv].PowerTables is null) continue;
                    if (Planet.variables[iv].PowerTables[it].Terms is null) continue;
                    TermF[] terms = Planet.variables[iv].PowerTables[it].Terms;
                    if (terms.Length < (int)vectorSize)
                    {
                        foreach (TermF term in terms)
                        {
                            u = term.B + term.C * phi;
                            (su, cu) = MathF.SinCos(u);
                            Result[iv] = Result[iv] + term.A * cu * tit;

                            // Original resolution specification.
                            if (Planet.version == VSOPVersion.VSOP87) continue;

                            // Derivative of 3 variables
                            if (it == 0)
                                Result[iv + 3] += (0 * it * term.A * cu) - (tit * term.A * term.C * su);
                            else
                                Result[iv + 3] += (t[it - 1] * it * term.A * cu) - (tit * term.A * term.C * su);
                        }
                    }
                    else
                    {
                        Span<float> span_a = new Span<float>(Planet.variables[iv].PowerTables[it].Array_A);
                        Span<float> span_b = new Span<float>(Planet.variables[iv].PowerTables[it].Array_B);
                        Span<float> span_c = new Span<float>(Planet.variables[iv].PowerTables[it].Array_C);
                        ref float ref_a = ref MemoryMarshal.GetReference<float>(Planet.variables[iv].PowerTables[it].Array_A);
                        ref float ref_b = ref MemoryMarshal.GetReference<float>(Planet.variables[iv].PowerTables[it].Array_B);
                        ref float ref_c = ref MemoryMarshal.GetReference<float>(Planet.variables[iv].PowerTables[it].Array_C);
                        int TableSize = terms.Length;
                        int SIMDLength = (terms.Length - vectorSize);
                        int Offset = 0;
                        // u = term.B + term.C * phi;
                        for (Offset = 0; Offset <= SIMDLength; Offset += vectorSize)
                        {
                            var vc = Vector256.LoadUnsafe(ref ref_c, (nuint)Offset);
                            var vb = Vector256.LoadUnsafe(ref ref_b, (nuint)Offset);
                            ((vc * phi) + vb).StoreUnsafe(ref ref_u, (nuint)Offset);
                        }
                        for (; Offset < TableSize; Offset++)
                        {
                            span_u[Offset] = (span_c[Offset] * phi) + span_b[Offset];
                        }

                        //(su, cu) = Math.SinCos(u);
                        for (Offset = 0; Offset < TableSize; Offset++)
                        {
                            span_su[Offset] = MathF.Sin(span_u[Offset]);
                            span_cu[Offset] = MathF.Cos(span_u[Offset]);
                        }

                        //term.A * cu * tit
                        for (Offset = 0; Offset <= SIMDLength; Offset += vectorSize)
                        {
                            var vcu = Vector256.LoadUnsafe(ref ref_cu, (nuint)Offset);
                            var va = Vector256.LoadUnsafe(ref ref_a, (nuint)Offset);
                            (va * vcu * tit).StoreUnsafe(ref ref_a_cu_tit, (nuint)Offset);
                        }
                        for (; Offset < TableSize; Offset++)
                        {
                            span_a_cu_tit[Offset] = span_a[Offset] * span_cu[Offset] * tit;
                        }

                        //Result[iv] += term.A * cu * tit;
                        sum ^= sum;
                        for (Offset = 0; Offset <= SIMDLength; Offset += vectorSize)
                        {
                            var v1 = Vector256.LoadUnsafe(ref ref_a_cu_tit, (nuint)Offset);
                            sum += v1;
                        }
                        Result[iv] += Vector256.Sum(sum);
                        for (; Offset < TableSize; Offset++)
                        {
                            Result[iv] += span_a_cu_tit[Offset];
                        }

                        if (Planet.version == VSOPVersion.VSOP87) continue;

                        //Right= tit * term.A * term.C * su
                        for (Offset = 0; Offset <= SIMDLength; Offset += vectorSize)
                        {
                            var vsu = Vector256.LoadUnsafe(ref ref_su, (nuint)Offset);
                            var va = Vector256.LoadUnsafe(ref ref_a, (nuint)Offset);
                            var vc = Vector256.LoadUnsafe(ref ref_c, (nuint)Offset);
                            (va * vc * vsu * tit).StoreUnsafe(ref ref_R, (nuint)Offset);
                        }
                        for (; Offset < TableSize; Offset++)
                        {
                            span_R[Offset] = span_a[Offset] * span_c[Offset] * span_su[Offset] * tit;
                        }

                        if (it == 0)
                        {
                            sum ^= sum;
                            for (Offset = 0; Offset <= SIMDLength; Offset += vectorSize)
                            {
                                var v1 = Vector256.LoadUnsafe(ref ref_R, (nuint)Offset);
                                sum += v1;
                            }
                            Result[iv + 3] -= Vector256.Sum(sum);
                            for (; Offset < TableSize; Offset++)
                            {
                                Result[iv + 3] -= span_R[Offset];
                            }
                        }
                        else
                        {
                            // LEFT = t[it - 1] * it * term.A * cu
                            sum ^= sum;
                            for (Offset = 0; Offset <= SIMDLength; Offset += vectorSize)
                            {
                                var va = Vector256.LoadUnsafe(ref ref_a, (nuint)Offset);
                                var vcu = Vector256.LoadUnsafe(ref ref_cu, (nuint)Offset);
                                var vr = Vector256.LoadUnsafe(ref ref_R, (nuint)Offset);
                                sum += (t[it - 1] * it * va * vcu - vr);
                            }
                            Result[iv + 3] += Vector256.Sum(sum);
                            for (; Offset < TableSize; Offset++)
                            {
                                Result[iv + 3] += t[it - 1] * it * span_a[Offset] * span_cu[Offset] - span_R[Offset];
                            }

                            //left-right
                        }
                    }
                }
            }

            // Original resolution specification.
            if (Planet.version == VSOPVersion.VSOP87) return Result.ToArray();
            for (int ic = 0; ic < 3; ic++)
            {
                Result[ic + 3] /= 365250f;
            }

            //Modulo Spherical longitude L into [0,2*pi)
            if (Utility.GetCoordinatesType(Planet.version) == CoordinatesType.Spherical)
            {
                ModuloCircle(ref Result[0]);
            }

            return Result.ToArray();
        }

        private DateTime Tinput;
        private VSOPTime vTime;
        private int tableIndex;

        [GlobalSetup]
        public void preset()
        {
            DateTime.TryParse("2023-01-01T00:00:00.0000000Z", CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.AdjustToUniversal, out Tinput);
            vTime = new VSOPTime(Tinput);
            tableIndex = VSOP87DATAF.FindIndex(x => x.version == VSOPVersion.VSOP87D && x.body == VSOPBody.EARTH);
        }

        [Benchmark]
        public float[] Test_Legacy()
        {
            var results = Calculate(VSOP87DATAF[tableIndex], (float)VSOPTime.ToJulianDate(vTime.TDB));
            return results;
        }

        [Benchmark]
        public float[] Test_SIMD()
        {
            var results = Calculate_SIMD(VSOP87DATAF[tableIndex], (float)VSOPTime.ToJulianDate(vTime.TDB));
            return results;
        }

        private void ModuloCircle(ref float RAD)
        {
            RAD -= MathF.Floor(RAD / 2 / MathF.PI) * 2 * MathF.PI;
            if (RAD < 0)
                RAD += 2 * MathF.PI;
        }
    }
}