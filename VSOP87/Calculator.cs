using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;

namespace VSOP87
{
    public class Calculator
    {
        public List<PlanetTable> VSOP87DATA;

        public Calculator()
        {
            VSOP87DATA = new List<PlanetTable>();
            using (MemoryStream ms = new MemoryStream(Resource.VSOP87DATA))
            {
                var bf = new BinaryFormatter();
                VSOP87DATA = (List<PlanetTable>)bf.Deserialize(ms);
            }
            
        }

        public double [] CalcPlanet(ref PlanetTable Planet, DateTime TDB)
        {
            return CalcPlanet(ref Planet, TimeConverter.ToJulianDate(TDB));
        }

        public double[] CalcPlanet(ref PlanetTable Planet, double JD)
        {
            double phi = (JD - 2451545.0d) / 365250d;

            double[] t = new double[6];
            for (int i = 0; i < 6; i++)
            {
                t[i] = Math.Pow(phi, i);
            }

            double[] Result = new double[6];
            double u, cu, su;
            for (int ic = 0; ic < 6; ic++)
            {
                for (int it = 5; it >= 0; it--)
                {
                    if (Planet.variables[ic].PowerTables == null) continue;
                    if (Planet.variables[ic].PowerTables[it].Terms == null) continue;
                    foreach (Term term in Planet.variables[ic].PowerTables[it].Terms)
                    {
                        u = term.B + term.C * phi;
                        cu = Math.Cos(u);
                        su = Math.Sin(u);
                        Result[ic] += (term.A * cu * t[it]);

                        // Original resolution specification.
                        if (Planet.iver == VSOPVersion.VSOP87) continue;

                        // Derivative for 3 variable
                        if (it == 0)
                            Result[ic + 3] += (0 * it * term.A * cu) - (t[it] * term.A * term.C * su);
                        else
                            Result[ic + 3] += (t[it - 1] * it * term.A * cu) - (t[it] * term.A * term.C * su);
                    }
                }
            }

            // Original resolution specification.
            if (Planet.iver == VSOPVersion.VSOP87) return Result;
            for (int ic = 0; ic < 3; ic++)
            {
                Result[ic + 3] /= 365250d;
            }
            return Result;
        }

    }
}