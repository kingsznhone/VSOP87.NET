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
            DataReader reader = new DataReader(@"D:\VSOP87DATA");
            reader.ReadPlanet(Version.VSOP87, Body.MERCURY);
            VSOPCalculator vsop = new VSOPCalculator(reader.planetdata);

            DateTime t = DateTime.Now;
            DateTime.TryParse("2000-01-01T12:00:00.0000000Z", out t);
            t = t.ToUniversalTime();
            //for (int i = 0; i<10; i++)
            {
                Console.WriteLine(t.ToString());
                double[] LBR = vsop.CalcPlanet(t);
                
                
                
                //LBR[0] = LBR[0] * 180 / Math.PI;
                //Console.WriteLine(Mod360(LBR[0]));
                //LBR[1] = LBR[1] * 180 / Math.PI;
                //Console.WriteLine(Mod360(LBR[0]));
                //Console.WriteLine(LBR[2]);

                foreach(double d in LBR)
                {
                    Console.WriteLine(ModPI(d));
                }
                t = t.AddDays(36525);

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

        static public double ModPI(double i)
        {
            while (i < -0.1) i += Math.PI;
            while (i > 2* Math.PI) i -=(2* Math.PI);
            return i;
        }
    }
}
