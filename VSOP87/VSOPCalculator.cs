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

        public VSOPCalculator(PlanetData planetDatas)
        {
            this.planetDatas = planetDatas;
        }

        private void SetTime(DateTime TDB)
        {
            double JD = ToJulianDate(TDB);
            this.phi = (JD - 2451545.0d) / 365250d;
        }

        private double ToJulianDate(DateTime TDB)
        {
            return TDB.ToOADate() + 2415018.5d;
        }

        public double[] CalcPlanet (DateTime TDB)
        {
            double[] LBR = new double[3];
            ParallelLoopResult result = Parallel.For(0, 3, ic =>
            {
                LBR[ic] = CalcIC(ic, TDB);
            });
            return LBR;
        }

        public double CalcIC(int ic,DateTime TDB)
        {
            SetTime(TDB);
            double[] R = new double[6];
            double r=0d;
           
            for(int it=5; it>=0;it--)
            {
                double counter = 0d;
                if (planetDatas.variables[ic].PowerTables[it].Terms == null) continue;
                foreach(Term term in planetDatas.variables[ic].PowerTables[it].Terms)
                {
                    counter+=term.A* Math.Cos(term.B + term.C * phi);
                }
                R[it] = counter;
            }

            r = (((((R[5] * phi + R[4]) * phi + R[3]) * phi + R[2]) * phi + R[1]) * phi + R[0]);
            
            return r;


        }

    }
}
