﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using FastLZMA2Net;
using MemoryPack;

namespace VSOP87
{
    public class Calculator
    {
        public readonly List<PlanetTable> VSOP87DATA;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector128<float> GetZero() => Vector128<float>.Zero;

        public Calculator()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream s = assembly.GetManifestResourceStream($"VSOP87.NET.Resources.VSOP87DATA.BIN");
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            VSOP87DATA = MemoryPackSerializer.Deserialize<List<PlanetTable>>(FL2.Decompress(ms.ToArray()));
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
                int tableIndex = VSOP87DATA.FindIndex(x => x.version == iver && x.body == ibody);

                double[] result = Calculate(VSOP87DATA[tableIndex], time.JulianDate);

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
#if NET8_0
            // Detail https://github.com/dotnet/runtime/issues/95954
            _ = GetZero();
#endif
            //double cu, su;
            for (int i = 0; i < 6; i++)
            {
                t[i] = Math.Pow(phi, i);
            }

            for (int iv = 0; iv < 6; iv++)
            {
                for (int it = 0; it <= 5; it++)
                {
                    if (Planet.variables[iv].PowerTables is null) continue;
                    if (Planet.variables[iv].PowerTables[it].Terms is null) continue;
                    Term[] terms = Planet.variables[iv].PowerTables[it].Terms;

                    for (int i = 0; i < terms.Length; i++)
                    {
                        (double su, double cu) = Math.SinCos(terms[i].B + terms[i].C * phi);
                        Result[iv] += terms[i].A * cu * t[it];

                        // Original resolution specification.
                        if (Planet.version == VSOPVersion.VSOP87) continue;

                        // Derivative of 3 variables

                        double dR = (t[it] * terms[i].A * terms[i].C * su);
                        if (it == 0)
                        {
                            Result[iv + 3] -= dR;
                        }
                        else
                        {
                            double dL = (t[it - 1] * it * terms[i].A * cu);
                            Result[iv + 3] += dL - dR;
                        }
                    }
                }
            }

            // Original resolution specification.
            if (Planet.version == VSOPVersion.VSOP87)
            {
                ModuloCircle(ref Result[1]);
                return Result.ToArray();
            }
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

        private void ModuloCircle(ref double RAD)
        {
            RAD = (RAD % Math.Tau + Math.Tau) % Math.Tau;
        }
    }
}