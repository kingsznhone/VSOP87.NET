using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using BenchmarkDotNet.Attributes;

namespace VSOP87
{
    [MemoryDiagnoser]
    public class CalculatorF
    {
        public readonly List<PlanetTableF> VSOP87DATAF;
        public readonly int vectorSize = Vector<float>.Count;

        float[] array_u = new float[2048];
        float[] array_cu = new float[2048];
        float[] array_su = new float[2048];
        float[] array_a_cu_tit = new float[2048];
        float[] array_a_c_su_tit = new float[2048];
        float[] array_tit_it_a_cu = new float[2048];
        float[] array_LR = new float[2048];
        public CalculatorF()
        {
            using (Stream ms = Assembly.GetExecutingAssembly().GetManifestResourceStream("VSOP87.NET.Resources.VSOP87DATAF.BIN"))
            {
                VSOP87DATAF = (List<PlanetTableF>)new BinaryFormatter().Deserialize(ms);
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

                        if(VSOP87DATAF[ip].variables[iv].PowerTables[it].Terms.Length >termmax)
                        {
                            termmax = VSOP87DATAF[ip].variables[iv].PowerTables[it].Terms.Length;
                        }
                        int RawLength = VSOP87DATAF[ip].variables[iv].PowerTables[it].Terms.Length;
                        int REM;
                        Math.DivRem(RawLength, vectorSize, out REM);
                        int SIMDLength = RawLength + (vectorSize - REM);
                        var terms = VSOP87DATAF[ip].variables[iv].PowerTables[it].Terms;

                        float[] buffer = new float[SIMDLength];
                        Array.Copy(terms.Select(x => x.A).ToArray(), buffer, terms.Length);
                        VSOP87DATAF[ip].variables[iv].PowerTables[it].Array_A = buffer;

                        buffer = new float[SIMDLength];
                        Array.Copy(terms.Select(x => x.B).ToArray(), buffer, terms.Length);
                        VSOP87DATAF[ip].variables[iv].PowerTables[it].Array_B = buffer;

                        buffer = new float[SIMDLength];
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

                float[] result_SIMD = Calculate_SIMD(VSOP87DATAF[tableIndex],(float) VSOPTime.ToJulianDate(time.TDB));
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

            float[] t = new float[6];
            for (int i = 0; i < 6; i++)
            {
                t[i] = MathF.Pow(phi, i);
            }

            float[] Result = new float[6];
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
            }

            // Original resolution specification.
            if (Planet.version == VSOPVersion.VSOP87) return Result;
            for (int ic = 0; ic < 3; ic++)
            {
                Result[ic + 3] /= 365250f;
            }

            //Modulo Spherical longitude L into [0,2*pi)
            if (Utility.GetCoordinatesType(Planet.version) == CoordinatesType.Spherical)
            {
                ModuloCircle(ref Result[0]);
            }

            return Result;
        }
        private float[] Calculate_SIMD(PlanetTableF Planet, float JD)
        {
            float phi = (JD - 2451545.0f) / 365250f;
            Vector<float> vphi = new Vector<float>(phi);

            float[] t = new float[6];
            for (int i = 0; i < 6; i++)
            {
                t[i] = MathF.Pow(phi, i);
            }

            float[] Result = new float[6];
            float tit;//t[it]
            for (int iv = 0; iv < 6; iv++)
            {
                for (int it = 5; it >= 0; it--)
                {
                    
                    Vector<float> vit = new Vector<float>(new float[] { it, it, it, it, it, it, it, it });
                    tit = t[it];
                    Vector<float> vtit = new Vector<float>(new float[] { tit, tit, tit, tit, tit, tit, tit, tit });
                    if (Planet.variables[iv].PowerTables is null) continue;
                    if (Planet.variables[iv].PowerTables[it].Terms is null) continue;

                    int SIMDLength = Planet.variables[iv].PowerTables[it].Array_A.Length;


                    // u = term.B + term.C * phi;
                    for (int i = 0; i < SIMDLength; i += vectorSize)
                    {
                        var vc = new Vector<float>(Planet.variables[iv].PowerTables[it].Array_C, i);
                        var vb = new Vector<float>(Planet.variables[iv].PowerTables[it].Array_B, i);
                        ((vc * vphi) + vb).CopyTo(array_u, i);
                    }

                    //(su, cu) = Math.SinCos(u);
                    for (int i = 0; i < SIMDLength; i++)
                    {
                        (array_su[i], array_cu[i]) = MathF.SinCos(array_u[i]);
                    }

                    //term.A * cu * tit
                    for (int i = 0; i < SIMDLength; i += vectorSize)
                    {
                        var vcu = new Vector<float>(array_cu, i);
                        var va = new Vector<float>(Planet.variables[iv].PowerTables[it].Array_A, i);
                        (va * vcu * vtit).CopyTo(array_a_cu_tit, i);
                    }

                    //Result[iv] += term.A * cu * tit;
                    for (int i = 0; i < SIMDLength; i += vectorSize)
                    {
                        var v1 = new Vector<float>(array_a_cu_tit, i);
                        Result[iv] += Vector.Sum(v1);
                    }

                    if (Planet.version == VSOPVersion.VSOP87) continue;

                    // tit * term.A * term.C * su
                    
                    for (int i = 0; i < SIMDLength; i += vectorSize)
                    {
                        var vsu = new Vector<float>(array_su, i);
                        var va = new Vector<float>(Planet.variables[iv].PowerTables[it].Array_A, i);
                        var vc = new Vector<float>(Planet.variables[iv].PowerTables[it].Array_C, i);
                        (-va * vc * vsu * vtit).CopyTo(array_a_c_su_tit, i);
                    }

                    if (it == 0)
                    {
                        for (int i = 0; i < SIMDLength; i += vectorSize)
                        {
                            var v1 = new Vector<float>(array_a_c_su_tit, i);
                            Result[iv + 3] += Vector.Sum(v1);
                        }
                    }
                    else
                    {
                        
                        Vector<float> vtit1 = new Vector<float>(new float[] { t[it - 1], t[it - 1], t[it - 1], t[it - 1], t[it - 1], t[it - 1], t[it - 1], t[it - 1] });

                        // t[it - 1] * it * term.A * cu
                        for (int i = 0; i < SIMDLength; i += vectorSize)
                        {
                            var va = new Vector<float>(Planet.variables[iv].PowerTables[it].Array_A, i);
                            var vcu = new Vector<float>(array_cu, i);
                            (vtit1 * vit * va * vcu).CopyTo(array_tit_it_a_cu, i);
                        }

                        //left-right
                        
                        for (int i = 0; i < SIMDLength; i += vectorSize)
                        {
                            var v1 = new Vector<float>(array_tit_it_a_cu, i);
                            var v2 = new Vector<float>(array_a_c_su_tit, i);
                            (v1 - v2).CopyTo(array_LR, i);
                        }

                        for (int i = 0; i < SIMDLength; i += vectorSize)
                        {
                            var v1 = new Vector<float>(array_LR, i);
                            Result[iv + 3] += Vector.Sum(v1);
                        }
                    }
                }
            }

            // Original resolution specification.
            if (Planet.version == VSOPVersion.VSOP87) return Result;
            for (int ic = 0; ic < 3; ic++)
            {
                Result[ic + 3] /= 365250f;
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
            tableIndex = VSOP87DATAF.FindIndex(x => x.version == VSOPVersion.VSOP87D && x.body == VSOPBody.EARTH);
        }

        [Benchmark]
        public float[] Test_Legacy()
        {
            float[] results = new float[6];
            foreach (VSOPVersion iv in Enum.GetValues(typeof(VSOPVersion)))
            {
                foreach (VSOPBody ib in Utility.AvailableBody(iv))
                {
                    tableIndex = VSOP87DATAF.FindIndex(x => x.version == iv && x.body == ib);
                    results = Calculate(VSOP87DATAF[tableIndex],(float) VSOPTime.ToJulianDate(vTime.TDB));
                }
            }
            return results;
        }

        [Benchmark]
        public float[] Test_SIMD()
        {
            float[] results = new float[6];
            foreach (VSOPVersion iv in Enum.GetValues(typeof(VSOPVersion)))
            {
                foreach (VSOPBody ib in Utility.AvailableBody(iv))
                {
                    tableIndex = VSOP87DATAF.FindIndex(x => x.version == iv && x.body == ib);
                    results = Calculate_SIMD(VSOP87DATAF[tableIndex], (float)VSOPTime.ToJulianDate(vTime.TDB));
                }
            }
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