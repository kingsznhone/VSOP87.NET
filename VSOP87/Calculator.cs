using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace VSOP87
{
    public class Calculator
    {
        public readonly List<PlanetTable> VSOP87DATA;

        public TimeSpan TimeUsed;

        private Stopwatch sw;

        public Calculator()
        {
            sw = new Stopwatch();
            TimeUsed = new TimeSpan(0);
            using (Stream ms = Assembly.GetExecutingAssembly().GetManifestResourceStream("VSOP87.Resources.VSOP87DATA.BIN"))
            {
                VSOP87DATA = (List<PlanetTable>)new BinaryFormatter().Deserialize(ms);
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="Planet">Dataset of a planet</param>
        /// <param name="JD">Julian Date</param>
        /// <returns></returns>
        private double[] Calculate(PlanetTable Planet, double JD)
        {
            sw.Restart();
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

            sw.Stop();
            TimeUsed = sw.Elapsed;
            return Result;
        }

        private void ModuloCircle(ref double RAD)
        {
            RAD -= Math.Floor(RAD / 2 / Math.PI) * 2 * Math.PI;
            if (RAD < 0)
                RAD += 2 * Math.PI;
        }
    }
}