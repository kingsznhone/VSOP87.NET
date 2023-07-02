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
    public class Calculator
    {
        public readonly List<PlanetTable> VSOP87DATA;
        public readonly int vectorSize = Vector256<double>.Count;

        private double[] array_u = new double[2048];
        private double[] array_cu = new double[2048];
        private double[] array_su = new double[2048];
        private double[] array_a_cu_tit = new double[2048];
        private double[] array_R = new double[2048];

        public Calculator()
        {
            using (Stream ms = Assembly.GetExecutingAssembly().GetManifestResourceStream("VSOP87.NET.Resources.VSOP87DATA.BIN"))
            {
#pragma warning disable SYSLIB0011 // 类型或成员已过时
                VSOP87DATA = (List<PlanetTable>)new BinaryFormatter().Deserialize(ms);
#pragma warning restore SYSLIB0011 // 类型或成员已过时
                Console.WriteLine("VSOP87DATA.BIN Loaded");
            }
            for (int ip = 0; ip < VSOP87DATA.Count; ip++)
            {
                for (int iv = 0; iv < 6; iv++)
                {
                    for (int it = 5; it >= 0; it--)
                    {
                        if (VSOP87DATA[ip].variables[iv].PowerTables is null) continue;
                        if (VSOP87DATA[ip].variables[iv].PowerTables[it].Terms is null) continue;

                        //Prepare SIMD Array
                        var terms = VSOP87DATA[ip].variables[iv].PowerTables[it].Terms;
                        double[] buffer = new Double[terms.Length];
                        Array.Copy(terms.Select(x => x.A).ToArray(), buffer, terms.Length);
                        VSOP87DATA[ip].variables[iv].PowerTables[it].Array_A = buffer;

                        buffer = new Double[terms.Length];
                        Array.Copy(terms.Select(x => x.B).ToArray(), buffer, terms.Length);
                        VSOP87DATA[ip].variables[iv].PowerTables[it].Array_B = buffer;

                        buffer = new Double[terms.Length];
                        Array.Copy(terms.Select(x => x.C).ToArray(), buffer, terms.Length);
                        VSOP87DATA[ip].variables[iv].PowerTables[it].Array_C = buffer;
                    }
                }
            }

            if (Vector.IsHardwareAccelerated)
            {
                Console.WriteLine("HWACCEL Enable.");
            }
            else
            {
                Console.WriteLine("HWACCEL Disable");
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
        public VSOPResult GetPlanet(VSOPBody ibody, VSOPVersion iver, VSOPTime time)
        {
            if (Utility.CheckAvailability(iver, ibody))
            {
                int tableIndex = VSOP87DATA.FindIndex(x => x.version == iver && x.body == ibody);

                double[] result = Calculate(VSOP87DATA[tableIndex], VSOPTime.ToJulianDate(time.TDB));

                switch (iver)
                {
                    case VSOPVersion.VSOP87:
                        return new VSOPResultELL(iver, ibody, time, result);

                    case VSOPVersion.VSOP87A or VSOPVersion.VSOP87C or VSOPVersion.VSOP87E:
                        return new VSOPResultXYZ(iver, ibody, time, result);

                    case VSOPVersion.VSOP87B or VSOPVersion.VSOP87D:
                        return new VSOPResultLBR(iver, ibody, time, result);

                    default: throw new ArgumentException();
                }
            }
            else
            {
                throw new ArgumentException("No body in this version.");
            }
        }

        public VSOPResult GetPlanet_SIMD(VSOPBody ibody, VSOPVersion iver, VSOPTime time)
        {
            if (Utility.CheckAvailability(iver, ibody))
            {
                int tableIndex = VSOP87DATA.FindIndex(x => x.version == iver && x.body == ibody);

                double[] result_SIMD = Calculate_SIMD(VSOP87DATA[tableIndex], VSOPTime.ToJulianDate(time.TDB));
                switch (iver)
                {
                    case VSOPVersion.VSOP87:
                        return new VSOPResultELL(iver, ibody, time, result_SIMD);

                    case VSOPVersion.VSOP87A or VSOPVersion.VSOP87C or VSOPVersion.VSOP87E:
                        return new VSOPResultXYZ(iver, ibody, time, result_SIMD);

                    case VSOPVersion.VSOP87B or VSOPVersion.VSOP87D:
                        return new VSOPResultLBR(iver, ibody, time, result_SIMD);

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
        private double[] Calculate(PlanetTable Planet, double JD)
        {
            double phi = (JD - 2451545.0d) / 365250d;

            Span<double> t = stackalloc double[6];
            for (int i = 0; i < 6; i++)
            {
                t[i] = Math.Pow(phi, i);
            }

            Span<double> Result = stackalloc double[6];
            double u, cu, su;
            double tit;//t[it]
            for (int iv = 0; iv < 6; iv++)
            {
                for (int it = 5; it >= 0; it--)
                {
                    tit = t[it];
                    if (Planet.variables[iv].PowerTables is null) continue;
                    if (Planet.variables[iv].PowerTables[it].Terms is null) continue;
                    foreach (Term term in Planet.variables[iv].PowerTables[it].Terms)
                    {
                        //u = b + c * phi
                        //su = Math.Sin(u);
                        //cu= Math.Cos(u);
                        (su, cu) = Math.SinCos(term.B + term.C * phi);
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
                Result[ic + 3] /= 365250d;
            }

            //Modulo Spherical longitude L into [0,2*pi)
            if (Utility.GetCoordinatesType(Planet.version) == CoordinatesType.Spherical)
            {
                ModuloCircle(ref Result[0]);
            }

            return Result.ToArray();
        }

        private double[] Calculate_SIMD(PlanetTable Planet, double JD)
        {
            double phi = (JD - 2451545.0d) / 365250d;

            Span<double> t = stackalloc double[6];
            for (int i = 0; i < 6; i++)
            {
                t[i] = Math.Pow(phi, i);
            }

            Span<double> Result = stackalloc double[6];
            Vector256<double> sum = new Vector256<double>();
            double u, cu, su;
            double tit;// =t[it]

            Span<double> span_u = new Span<double>(array_u);
            Span<double> span_cu = new Span<double>(array_cu);
            Span<double> span_su = new Span<double>(array_su);
            Span<double> span_a_cu_tit = new Span<double>(array_a_cu_tit);
            Span<double> span_R = new Span<double>(array_R);

            ref double ref_u = ref MemoryMarshal.GetReference<double>(array_u);
            ref double ref_cu = ref MemoryMarshal.GetReference<double>(array_cu);
            ref double ref_su = ref MemoryMarshal.GetReference<double>(array_su);
            ref double ref_a_cu_tit = ref MemoryMarshal.GetReference<double>(array_a_cu_tit);
            ref double ref_R = ref MemoryMarshal.GetReference<double>(array_R);

            for (int iv = 0; iv < 6; iv++)
            {
                for (int it = 5; it >= 0; it--)
                {
                    tit = t[it];
                    if (Planet.variables[iv].PowerTables is null) continue;
                    if (Planet.variables[iv].PowerTables[it].Terms is null) continue;
                    Term[] terms = Planet.variables[iv].PowerTables[it].Terms;
                    if (terms.Length < (int)vectorSize)
                    {
                        foreach (Term term in terms)
                        {
                            u = term.B + term.C * phi;
                            (su, cu) = Math.SinCos(u);
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
                        Span<double> span_a = new Span<double>(Planet.variables[iv].PowerTables[it].Array_A);
                        Span<double> span_b = new Span<double>(Planet.variables[iv].PowerTables[it].Array_B);
                        Span<double> span_c = new Span<double>(Planet.variables[iv].PowerTables[it].Array_C);
                        ref double ref_a = ref MemoryMarshal.GetReference<double>(Planet.variables[iv].PowerTables[it].Array_A);
                        ref double ref_b = ref MemoryMarshal.GetReference<double>(Planet.variables[iv].PowerTables[it].Array_B);
                        ref double ref_c = ref MemoryMarshal.GetReference<double>(Planet.variables[iv].PowerTables[it].Array_C);
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
                            span_su[Offset] = Math.Sin(span_u[Offset]);
                            span_cu[Offset] = Math.Cos(span_u[Offset]);
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
                Result[ic + 3] /= 365250d;
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
            tableIndex = VSOP87DATA.FindIndex(x => x.version == VSOPVersion.VSOP87D && x.body == VSOPBody.EARTH);
        }

        [Benchmark]
        public double[] Test_Legacy()
        {
            var results = Calculate(VSOP87DATA[tableIndex], VSOPTime.ToJulianDate(vTime.TDB));
            return results;
        }

        [Benchmark]
        public double[] Test_SIMD()
        {
            var results = Calculate_SIMD(VSOP87DATA[tableIndex], VSOPTime.ToJulianDate(vTime.TDB));
            return results;
        }

        private void ModuloCircle(ref double RAD)
        {
            RAD -= Math.Floor(RAD / 2 / Math.PI) * 2 * Math.PI;
            if (RAD < 0)
                RAD += 2 * Math.PI;
        }
    }
}