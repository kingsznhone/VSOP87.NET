using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSOP87
{
    class VSOPCalculator
    {
        PlanetData planetDatas;
        private double phi;
        private double[] t;

        public VSOPCalculator(PlanetData planetDatas)
        {
            this.planetDatas = planetDatas;
        }

        private void SetTime(DateTime TDB)
        {
            double JD = ToJulianDate(TDB);
            phi = (JD - 2451545.0d) / 365250d;

            t = new double[6];
            for (int i = 0; i < 6; i++)
            {
                t[i] = Math.Pow(phi, i);
            }
        }

        private double ToJulianDate(DateTime TDB)
        {
            return TDB.ToOADate() + 2415018.5d;
        }

        public double[] CalcPlanet(DateTime TDB)
        {
            SetTime(TDB);
            double[] r = new double[6];
            double u = 0d;
            double cu = 0d;
            double su = 0d;
            for (int ic = 0; ic < 6; ic++)
            {
                for (int it = 5; it >= 0; it--)
                {
                    if (planetDatas.variables[ic].PowerTables == null) continue;
                    if (planetDatas.variables[ic].PowerTables[it].Terms == null) continue;
                    foreach (Term term in planetDatas.variables[ic].PowerTables[it].Terms)
                    {
                        u = term.B + term.C * phi;
                        cu = Math.Cos(u);
                        su = Math.Sin(u);
                        r[ic] = r[ic] + (term.A * cu * t[it]);

                        // Original resolution specification.
                        if (planetDatas.iver == Version.VSOP87) continue;

                        // Derivative for 3 variable
                        if (it == 0)
                            r[ic + 3] += (0 * it * term.A * cu) - (t[it] * term.A * term.C * su);
                        else
                            r[ic + 3] += (t[it - 1] * it * term.A * cu) - (t[it] * term.A * term.C * su);
                    }

                }
            }

            // Original resolution specification.
            if (planetDatas.iver == Version.VSOP87) return r;

            for (int ic = 0; ic < 3; ic++)
            {
                r[ic + 3] /= 365250d;
            }
            return r;
        }


    }
}
