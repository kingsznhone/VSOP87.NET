using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace VSOP87
{
    [MemoryDiagnoser]
    public class Calculator
    {
        public readonly List<PlanetTable> VSOP87DATA;

        public readonly int vectorSize = Vector<double>.Count;

        double[] array_u = new double[2048];
        double[] array_cu = new double[2048];
        double[] array_su = new double[2048];
        double[] array_a_cu_tit = new double[2048];
        double[] array_a_c_su_tit = new double[2048];
        double[] array_tit_it_a_cu = new double[2048];
        double[] array_LR = new double[2048];
        public Calculator()
        {
            using (Stream ms = Assembly.GetExecutingAssembly().GetManifestResourceStream("VSOP87.NET.Resources.VSOP87DATA.BIN"))
            {
                VSOP87DATA = (List<PlanetTable>)new BinaryFormatter().Deserialize(ms);
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
                        int RawLength = VSOP87DATA[ip].variables[iv].PowerTables[it].Terms.Length;
                        int REM;
                        Math.DivRem(RawLength, vectorSize, out REM);
                        int SIMDLength = RawLength + (vectorSize - REM);
                        var terms = VSOP87DATA[ip].variables[iv].PowerTables[it].Terms;

                        double[] buffer = new Double[SIMDLength];
                        Array.Copy(terms.Select(x => x.A).ToArray(), buffer, terms.Length);
                        VSOP87DATA[ip].variables[iv].PowerTables[it].Array_A = buffer;

                        buffer = new Double[SIMDLength];
                        Array.Copy(terms.Select(x => x.B).ToArray(), buffer, terms.Length);
                        VSOP87DATA[ip].variables[iv].PowerTables[it].Array_B = buffer;

                        buffer = new Double[SIMDLength];
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

            double[] t = new double[6];
            for (int i = 0; i < 6; i++)
            {
                t[i] = Math.Pow(phi, i);
            }

            double[] Result = new double[6];
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
            }

            // Original resolution specification.
            if (Planet.version == VSOPVersion.VSOP87) return Result;
            for (int ic = 0; ic < 3; ic++)
            {
                Result[ic + 3] /= 365250d;
            }

            //Modulo Spherical longitude L into [0,2*pi)
            if (Utility.GetCoordinatesType(Planet.version) == CoordinatesType.Spherical)
            {
                ModuloCircle(ref Result[0]);
            }

            return Result;
        }

        private double[] Calculate_SIMD(PlanetTable Planet, double JD)
        {
            double phi = (JD - 2451545.0d) / 365250d;
            Vector<double> vphi = new Vector<double>(new double[] { phi, phi, phi, phi });

            double[] t = new double[6];
            for (int i = 0; i < 6; i++)
            {
                t[i] = Math.Pow(phi, i);
            }

            double[] Result = new double[6];
            double u, cu, su;
            double tit;//t[it]
            for (int iv = 0; iv < 6; iv++)
            {
                for (int it = 5; it >= 0; it--)
                {
                    Vector<double> vit = new Vector<double>(new double[] { it, it, it, it });
                    tit = t[it];
                    Vector<double> vtit = new Vector<double>(new double[] { tit, tit, tit, tit });
                    if (Planet.variables[iv].PowerTables is null) continue;
                    if (Planet.variables[iv].PowerTables[it].Terms is null) continue;

                    int SIMDLength = Planet.variables[iv].PowerTables[it].Array_A.Length;

                    // u = term.B + term.C * phi;
                    for (int i = 0; i < SIMDLength; i += vectorSize)
                    {
                        var vc = new Vector<double>(Planet.variables[iv].PowerTables[it].Array_C, i);
                        var vb = new Vector<double>(Planet.variables[iv].PowerTables[it].Array_B, i);
                        ((vc * vphi) + vb).CopyTo(array_u, i);
                    }

                    //(su, cu) = Math.SinCos(u);
                    for (int i = 0; i < SIMDLength; i++)
                    {
                        (array_su[i], array_cu[i]) = Math.SinCos(array_u[i]);
                    }

                    //term.A * cu * tit
                    for (int i = 0; i < SIMDLength; i += vectorSize)
                    {
                        var vcu = new Vector<double>(array_cu, i);
                        var va = new Vector<double>(Planet.variables[iv].PowerTables[it].Array_A, i);
                        (va * vcu * vtit).CopyTo(array_a_cu_tit, i);
                    }

                    //Result[iv] += term.A * cu * tit;
                    for (int i = 0; i < SIMDLength; i += vectorSize)
                    {
                        var v1 = new Vector<double>(array_a_cu_tit, i);
                        Result[iv] += Vector.Sum(v1);
                    }

                    if (Planet.version == VSOPVersion.VSOP87) continue;

                    //Right= tit * term.A * term.C * su
                    for (int i = 0; i < SIMDLength; i += vectorSize)
                    {
                        var vsu = new Vector<double>(array_su, i);
                        var va = new Vector<double>(Planet.variables[iv].PowerTables[it].Array_A, i);
                        var vc = new Vector<double>(Planet.variables[iv].PowerTables[it].Array_C, i);
                        (-va * vc * vsu * vtit).CopyTo(array_a_c_su_tit, i);
                    }

                    if (it == 0)
                    {
                        for (int i = 0; i < SIMDLength; i += vectorSize)
                        {
                            var v1 = new Vector<double>(array_a_c_su_tit, i);
                            Result[iv + 3] += Vector.Sum(v1);
                        }
                    }
                    else
                    {

                        Vector<double> vtit1 = new Vector<double>(new double[] { t[it - 1], t[it - 1], t[it - 1], t[it - 1] });

                        // LEFT = t[it - 1] * it * term.A * cu
                        for (int i = 0; i < SIMDLength; i += vectorSize)
                        {
                            var va = new Vector<double>(Planet.variables[iv].PowerTables[it].Array_A, i);
                            var vcu = new Vector<double>(array_cu, i);
                            
                            (vtit1 * vit * va * vcu).CopyTo(array_tit_it_a_cu, i);
                        }

                        //left-right
                        for (int i = 0; i < SIMDLength; i += vectorSize)
                        {
                            var v1 = new Vector<double>(array_tit_it_a_cu, i);
                            var v2 = new Vector<double>(array_a_c_su_tit, i);
                            (v1 - v2).CopyTo(array_LR, i);
                        }

                        for (int i = 0; i < SIMDLength; i += vectorSize)
                        {
                            var v1 = new Vector<double>(array_LR, i);
                            Result[iv + 3] += Vector.Sum(v1);
                        }
                    }
                }
            }

            // Original resolution specification.
            if (Planet.version == VSOPVersion.VSOP87) return Result;
            for (int ic = 0; ic < 3; ic++)
            {
                Result[ic + 3] /= 365250d;
            }

            //Modulo Spherical longitude L into [0,2*pi)
            if (Utility.GetCoordinatesType(Planet.version) == CoordinatesType.Spherical)
            {
                ModuloCircle(ref Result[0]);
            }

            return Result;
        }



        DateTime Tinput;
        VSOPTime vTime;
        int tableIndex;
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
            double[] results = new double[6];
            foreach (VSOPVersion iv in Enum.GetValues(typeof(VSOPVersion)))
            {
                foreach (VSOPBody ib in Utility.AvailableBody(iv))
                {
                    tableIndex = VSOP87DATA.FindIndex(x => x.version == iv && x.body == ib);
                    results = Calculate(VSOP87DATA[tableIndex], VSOPTime.ToJulianDate(vTime.TDB));
                }
            }
            return results;
        }

        [Benchmark]
        public double[] Test_SIMD()
        {
            double[] results = new double[6];
            foreach (VSOPVersion iv in Enum.GetValues(typeof(VSOPVersion)))
            {
                foreach (VSOPBody ib in Utility.AvailableBody(iv))
                {
                    tableIndex = VSOP87DATA.FindIndex(x => x.version == iv && x.body == ib);
                    results = Calculate_SIMD(VSOP87DATA[tableIndex], VSOPTime.ToJulianDate(vTime.TDB));
                }
            }
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