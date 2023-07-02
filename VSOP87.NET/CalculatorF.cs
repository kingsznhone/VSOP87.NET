using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
#if BENCHMARK
using BenchmarkDotNet.Attributes;
#endif

namespace VSOP87
{
    public class CalculatorF
    {
        public readonly List<PlanetTableF> VSOP87DATAF;

        public CalculatorF()
        {
            using (Stream ms = Assembly.GetExecutingAssembly().GetManifestResourceStream("VSOP87.NET.Resources.VSOP87DATAF.BIN"))
            {
#pragma warning disable SYSLIB0011 //
                VSOP87DATAF = (List<PlanetTableF>)new BinaryFormatter().Deserialize(ms);
#pragma warning restore SYSLIB0011 //
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
        public VSOPResultF GetPlanetPosition(VSOPBody ibody, VSOPVersion iver, VSOPTime time)
        {
            if (Utility.CheckAvailability(iver, ibody))
            {
                int tableIndex = VSOP87DATAF.FindIndex(x => x.version == iver && x.body == ibody);

                float[] result = Calculate(VSOP87DATAF[tableIndex], (float)VSOPTime.ToJulianDate(time.TDB));

                switch (iver)
                {
                    case VSOPVersion.VSOP87:
                        return new VSOPResultF_ELL(iver, ibody, time, result);

                    case VSOPVersion.VSOP87A or VSOPVersion.VSOP87C or VSOPVersion.VSOP87E:
                        return new VSOPResultF_XYZ(iver, ibody, time, result);

                    case VSOPVersion.VSOP87B or VSOPVersion.VSOP87D:
                        return new VSOPResultF_LBR(iver, ibody, time, result);

                    default: throw new ArgumentException();
                }
            }
            else
            {
                throw new ArgumentException("No body in this version.");
            }
        }

        public async Task<VSOPResultF> GetPlanetPositionAsync(VSOPBody ibody, VSOPVersion iver, VSOPTime time)
        {
            return await Task.Run(() => GetPlanetPosition(ibody, iver, time));
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
            Span<float> Result = stackalloc float[6];
            Span<float> t = stackalloc float[6];
            float cu, su;
            for (int i = 0; i < 6; i++)
            {
                t[i] = MathF.Pow(phi, i);
            }
            for (int iv = 0; iv < 6; iv++)
            {
                for (int it = 5; it >= 0; it--)
                {
                    if (Planet.variables[iv].PowerTables is null) continue;
                    if (Planet.variables[iv].PowerTables[it].Terms is null) continue;
                    foreach (TermF term in Planet.variables[iv].PowerTables[it].Terms)
                    {
                        (su, cu) = MathF.SinCos(term.B + term.C * phi);
                        Result[iv] = Result[iv] + term.A * cu * t[it];

                        // Original resolution specification.
                        if (Planet.version == VSOPVersion.VSOP87) continue;

                        // Derivative of 3 variables
                        if (it == 0)
                            Result[iv + 3] += 0 - (t[it] * term.A * term.C * su);
                        else
                            Result[iv + 3] += (t[it - 1] * it * term.A * cu) - (t[it] * term.A * term.C * su);
                    }
                }
            }

            // Original resolution specification.
            if (Planet.version == VSOPVersion.VSOP87)
            {
                ModuloCircle(ref Result[1]);
                ModuloCircle(ref Result[2]);
                ModuloCircle(ref Result[3]);
                ModuloCircle(ref Result[4]);
                ModuloCircle(ref Result[5]);
            }
            else if (Utility.GetCoordinatesType(Planet.version) == CoordinatesType.Rectangular)
            {
                for (int ic = 0; ic < 3; ic++)
                {
                    Result[ic + 3] /= 365250f;
                }
            }
            //Modulo Spherical longitude L,B into [0,2*pi)
            else if (Utility.GetCoordinatesType(Planet.version) == CoordinatesType.Spherical)
            {
                ModuloCircle(ref Result[0]);
                ModuloCircle(ref Result[1]);
            }
            return Result.ToArray();
        }

        private void ModuloCircle(ref float RAD)
        {
            RAD -= MathF.Floor(RAD / MathF.Tau) * MathF.Tau;
            if (RAD < 0)
                RAD += MathF.Tau;
        }
        #if BENCHMARK
        #region Performance Test

        private DateTime Tinput;
        private VSOPTime vTime;
        private int tableIndex;

        [GlobalSetup]
        public void PerfTest_Init()
        {
            DateTime.TryParse("2023-01-01T00:00:00.0000000Z", CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.AdjustToUniversal, out Tinput);
            vTime = new VSOPTime(Tinput);
            tableIndex = VSOP87DATAF.FindIndex(x => x.version == VSOPVersion.VSOP87D && x.body == VSOPBody.EARTH);
        }

        [Benchmark]
        public float[] PerfTest()
        {
            var results = Calculate(VSOP87DATAF[tableIndex], (float)VSOPTime.ToJulianDate(vTime.TDB));
            return results;
        }

        #endregion Performance Test
        #endif
    }
}