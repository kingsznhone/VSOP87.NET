using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace VSOP87
{
    public class CalculatorF
    {
        public readonly List<PlanetTableF> VSOP87DATAF;

        public TimeSpan TimeUsed;

        private Stopwatch sw;

        public CalculatorF()
        {
            sw = new Stopwatch();
            TimeUsed = new TimeSpan(0);
            using (Stream ms = Assembly.GetExecutingAssembly().GetManifestResourceStream("VSOP87.Resources.VSOP87DATAF.BIN"))
            {
                VSOP87DATAF = (List<PlanetTableF>)new BinaryFormatter().Deserialize(ms);
                Console.WriteLine("VSOP87DATAF.BIN Loaded");
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="Planet">Dataset of a planet</param>
        /// <param name="JD">Julian Date</param>
        /// <returns></returns>
        private float[] Calculate(PlanetTableF Planet, float JD)
        {
            sw.Restart();
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

            sw.Stop();
            TimeUsed = sw.Elapsed;
            return Result;
        }

        private void ModuloCircle(ref float RAD)
        {
            RAD -= MathF.Floor(RAD / 2 / MathF.PI) * 2 * MathF.PI;
            if (RAD < 0)
                RAD += 2 * MathF.PI;
        }
    }
}