using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSOP87
{
    class Program
    {
        static void Main(string[] args)
        {
            DataReader reader = new DataReader(@"D:\VSOP87DATA\");
            VSOPCalculator vsop = new VSOPCalculator(reader.planet);
            DateTime t = DateTime.Now;
            DateTime.TryParse("1799-12-30T12:00:00.0000000Z", out t);
            t = t.ToUniversalTime();
            //t = t.AddSeconds(-492);
            //for (int i = 0; i<365; i++)
            {
                //t = t.AddDays(1);
                Console.WriteLine(t.ToString());
                double[] LBR = vsop.CalcPlanet(t);
                LBR[0] = LBR[0] * 180 / Math.PI;
                Console.WriteLine(Mod360(LBR[0]));
                LBR[1] = LBR[1] * 180 / Math.PI;
                Console.WriteLine(Mod360(LBR[0]));
                Console.WriteLine(LBR[2]);


                Console.WriteLine();
            }




            Console.ReadLine();
        }

        static public double Mod360(double i)
        {
            while (i < 0) i += 360;
            while (i > 360) i -= 360;
            return i;
        }
    }
}
