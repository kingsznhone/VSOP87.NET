using MemoryPack;
using System.IO.Compression;

namespace VSOP87
{
    public class Calculator
    {
        public readonly Dictionary<(VSOPVersion, VSOPBody), PlanetTable> VSOP87DATA;

        public Calculator()
        {
            //Import Planet Data
            string dataFilePath = Path.Combine(AppContext.BaseDirectory, "VSOP87.BR");
            using (MemoryStream recoveryStream = new MemoryStream())
            {
                using (FileStream fs = new FileStream(dataFilePath, FileMode.Open, FileAccess.Read))
                {
                    using (BrotliStream bs = new BrotliStream(fs, CompressionMode.Decompress))
                    {
                        bs.CopyTo(recoveryStream);
                    }
                }

                VSOP87DATA = MemoryPackSerializer.Deserialize<List<PlanetTable>>(recoveryStream.ToArray())
                .ToDictionary(t => (t.Version, t.Body));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ibody">VSOP87 Planet</param>
        /// <param name="iver">VSOP87 Version</param>
        /// <param name="TDB">Barycentric Dynamical Time</param>
        /// <returns>Result contain version, body, coordinates reference/type, time frame,and variables</returns>
        /// <exception cref="ArgumentException"></exception>
        public VSOPResult GetPlanetPosition(VSOPBody ibody, VSOPVersion iver, VSOPTime time)
        {
            if (Utility.CheckAvailability(iver, ibody))
            {
                if (!VSOP87DATA.TryGetValue((iver, ibody), out PlanetTable table))
                    throw new ArgumentException("No body in this version.");

                double[] result = Calculate(table, time.JulianDate);

                switch (iver)
                {
                    case VSOPVersion.VSOP87:
                        return new VSOPResult_ELL(ibody, time, result, FrameType.Dynamical, Epoch.J2000);

                    case VSOPVersion.VSOP87A:
                        return new VSOPResult_XYZ(ibody, time, result, FrameType.Dynamical, Epoch.J2000);

                    case VSOPVersion.VSOP87B:
                        return new VSOPResult_LBR(ibody, time, result, FrameType.Dynamical, Epoch.J2000);

                    case VSOPVersion.VSOP87C:
                        return new VSOPResult_XYZ(ibody, time, result, FrameType.Dynamical, Epoch.OfDate);

                    case VSOPVersion.VSOP87D:
                        return new VSOPResult_LBR(ibody, time, result, FrameType.Dynamical, Epoch.OfDate);

                    case VSOPVersion.VSOP87E:
                        return new VSOPResult_XYZ(ibody, time, result, FrameType.Barycentric, Epoch.J2000);

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
        /// <param name="ibody">VSOP87 Planet</param>
        /// <param name="iver">VSOP87 Version</param>
        /// <param name="TDB">Barycentric Dynamical Time</param>
        /// <returns>Result contain version, body, coordinates reference/type, time frame,and variables</returns>
        /// <exception cref="ArgumentException"></exception>
        public Task<VSOPResult> GetPlanetPositionAsync(VSOPBody ibody, VSOPVersion iver, VSOPTime time)
        {
            return Task.Run(() => GetPlanetPosition(ibody, iver, time));
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

            for (int i = 0; i < 6; i++)
            {
                t[i] = Math.Pow(phi, i);
            }

            foreach (VSOPVariable iv in Enum.GetValues<VSOPVariable>())
            {
                if (!Planet.variables.TryGetValue(iv, out VariableTable variableTable)) continue;
                for (int it = 0; it <= 5; it++)
                {
                    if (variableTable.PowerTables is null) continue;
                    if (variableTable.PowerTables[it] is null) continue;
                    ref readonly Term[] terms = ref variableTable.PowerTables[it].Terms;

                    int resultIdx = (int)iv - 1;
                    for (int i = 0; i < terms.Length; i++)
                    {
                        double u = terms[i].B + terms[i].C * phi;
                        double su = Math.Sin(u);
                        double cu = Math.Cos(u);
                        Result[resultIdx] += terms[i].A * cu * t[it];

                        // Original resolution specification.
                        if (Planet.Version == VSOPVersion.VSOP87) continue;

                        // Derivative of 3 variables

                        double dR = (t[it] * terms[i].A * terms[i].C * su);
                        if (it == 0)
                        {
                            Result[resultIdx + 3] -= dR;
                        }
                        else
                        {
                            double dL = (t[it - 1] * it * terms[i].A * cu);
                            Result[resultIdx + 3] += dL - dR;
                        }
                    }
                }
            }

            // Original resolution specification.
            if (Planet.Version == VSOPVersion.VSOP87)
            {
                ModuloCircle(ref Result[1]);
                return Result.ToArray();
            }
            for (int ic = 0; ic < 3; ic++)
            {
                Result[ic + 3] /= 365250d;
            }
            //Modulo Spherical longitude L into [0,2*pi)
            if (Utility.GetCoordinatesType(Planet.Version) == CoordinatesType.Spherical)
            {
                ModuloCircle(ref Result[0]);
            }
            return Result.ToArray();
        }

        private void ModuloCircle(ref double RAD)
        {
            RAD = (RAD % Math.Tau + Math.Tau) % Math.Tau;
        }
    }
}