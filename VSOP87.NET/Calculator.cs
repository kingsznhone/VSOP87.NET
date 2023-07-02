using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using BenchmarkDotNet.Attributes;

namespace VSOP87
{
    public class Calculator
    {
        public readonly List<PlanetTable> VSOP87DATA;

        public Calculator()
        {
            var debug = this.GetType().Assembly.GetManifestResourceNames();
            using (Stream ms = Assembly.GetExecutingAssembly().GetManifestResourceStream("VSOP87.NET.Resources.VSOP87DATA.BIN"))
            {
#pragma warning disable SYSLIB0011
                VSOP87DATA = (List<PlanetTable>)new BinaryFormatter().Deserialize(ms);
#pragma warning restore SYSLIB0011
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
        public VSOPResult GetPlanetPosition(VSOPBody ibody, VSOPVersion iver, VSOPTime time)
        {
            if (Utility.CheckAvailability(iver, ibody))
            {
                int tableIndex = VSOP87DATA.FindIndex(x => x.version == iver && x.body == ibody);

                double[] result = Calculate(VSOP87DATA[tableIndex], VSOPTime.ToJulianDate(time.TDB));

                switch (iver)
                {
                    case VSOPVersion.VSOP87:
                        return new VSOPResult_ELL(iver, ibody, time, result);

                    case VSOPVersion.VSOP87A or VSOPVersion.VSOP87C or VSOPVersion.VSOP87E:
                        return new VSOPResult_XYZ(iver, ibody, time, result);

                    case VSOPVersion.VSOP87B or VSOPVersion.VSOP87D:
                        return new VSOPResult_LBR(iver, ibody, time, result);

                    default: throw new ArgumentException();
                }
            }
            else
            {
                throw new ArgumentException("No body in this version.");
            }
        }

        public async Task<VSOPResult> GetPlanetPositionAsync(VSOPBody ibody, VSOPVersion iver, VSOPTime time)
        {
            return await Task.Run(() => GetPlanetPosition(ibody, iver, time));
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
            Span<double> Result = stackalloc double[6];
            Span<double> t = stackalloc double[6];
            double cu, su;
            for (int i = 0; i < 6; i++)
            {
                t[i] = Math.Pow(phi, i);
            }
            for (int iv = 0; iv < 6; iv++)
            {
                for (int it = 5; it >= 0; it--)
                {
                    if (Planet.variables[iv].PowerTables is null) continue;
                    if (Planet.variables[iv].PowerTables[it].Terms is null) continue;
                    foreach (Term term in Planet.variables[iv].PowerTables[it].Terms)
                    {
                        (su, cu) = Math.SinCos(term.B + term.C * phi);
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
                    Result[ic + 3] /= 365250d;
                }
            }
            //Modulo Spherical longitude L,B,l',B' into [0,2*pi)
            else if (Utility.GetCoordinatesType(Planet.version) == CoordinatesType.Spherical)
            {
                ModuloCircle(ref Result[0]);
                ModuloCircle(ref Result[1]);
                ModuloCircle(ref Result[3]);
                ModuloCircle(ref Result[4]);
            }
            return Result.ToArray();
        }

        private void ModuloCircle(ref double RAD)
        {
            RAD -= Math.Floor(RAD / Math.Tau) * Math.Tau;
            if (RAD < 0)
                RAD += Math.Tau;
        }

        #region Performance Test

        private DateTime Tinput;
        private VSOPTime vTime;
        private int tableIndex;

        [GlobalSetup]
        public void PerfTest_Init()
        {
            DateTime.TryParse("2023-01-01T00:00:00.0000000Z", CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.AdjustToUniversal, out Tinput);
            vTime = new VSOPTime(Tinput);
            tableIndex = VSOP87DATA.FindIndex(x => x.version == VSOPVersion.VSOP87D && x.body == VSOPBody.EARTH);
        }

        [Benchmark]
        public double[] PerfTest()
        {
            var results = Calculate(VSOP87DATA[tableIndex], VSOPTime.ToJulianDate(vTime.TDB));
            return results;
        }

        #endregion Performance Test
    }
}