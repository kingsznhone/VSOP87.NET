using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSOP87
{
    public enum Body
    {
        SUN = 0,
        MERCURY = 1,
        VENUS = 2,
        EARTH = 3,
        MARS = 4,
        JUPITER = 5,
        SATURN = 6,
        URANUS = 7,
        NEPTUNE = 8,
        EMB = 9
    }

    public enum Version
    {
        VSOP87 = 0,
        VSOP87A = 1,
        VSOP87B = 2,
        VSOP87C = 3,
        VSOP87D = 4,
        VSOP87E = 5,
    }


    public struct PlanetData
    {
        public Variable[] variables;
    }
    public struct Variable
    {
        public Power[] PowerTables;
    }
    public struct Power
    {
        public Term[] Terms;
    }

    public struct Header
    {
        public int iv; //VSOP87 version
        public string body; //name of body
        public int ib;
        public int ic; //index of coordinate
        public int it; //degree alpha of time variable T 
        public int nt; //number of terms of series
    }

    public struct Term
    {
        public int rank; //rank of the term in a serie
        public double A; //amplitude A
        public double B; //phase     B   
        public double C; //frequency C
    }


    public class DataReader
    {
        static string[] EXT = {@".sun",@".mer",@".ven",@".ear",@".mar",@".jup",@".sat",@".ura",@".nep",@".emb"};
        string Path;
        public PlanetData planet;
        public DataReader(string Data)
        {
            Path = @"D:\VSOP87DATA\VSOP87D.ear";

            planet.variables = new Variable[6];
            for (int ic = 0; ic < 6; ic++)
            {
                planet.variables[ic].PowerTables = new Power[6];
            }
            ReadPlanet(Path);
            Console.WriteLine("Load OK");
        }



        private string JointFilename(int iver,int ibody)
        {
            string buffer = Enum.GetName(typeof(Version), iver);
            buffer += EXT[ibody];
            return buffer;
        }

        public void ReadPlanet(string path)
        {
            StreamReader sr;
            Header H = new Header();
            string line;
            {
                //  C:\VSOPDATA\VSOP2013p1.dat
                sr = new StreamReader(path);
                while ((line = sr.ReadLine()) != null)
                {
                    ReadHeader(line, ref H);
                    Term[] buffer = new Term[H.nt];
                    for (int i = 0; i < H.nt; i++)
                    {
                        line = sr.ReadLine();
                        ReadTerm(line, ref buffer[i]);
                    }

                    planet.variables[H.ic].PowerTables[H.it].Terms = buffer;
                }
            }
            sr.Close();
        }

        private void ReadHeader(string line, ref Header H)
        {

            int lineptr = 17;
            H.iv = Convert.ToInt32(line.Substring(lineptr, 1).Trim()) - 1;
            lineptr += 5;

            H.body = line.Substring(lineptr, 7).Trim();
            lineptr += 19;
            H.ib = (int)Enum.Parse(typeof(Body), H.body);
            H.ic = Convert.ToInt32(line.Substring(lineptr, 1).Trim()) - 1;
            lineptr += 18;

            H.it = Convert.ToInt32(line.Substring(lineptr, 1).Trim());
            lineptr += 1;

            H.nt = Convert.ToInt32(line.Substring(lineptr, 7).Trim());
        }

        private Term ReadTerm(string line, ref Term T)
        {
            int lineptr;

            double abc;

            //
            lineptr = 5;
            T.rank = Convert.ToInt32(line.Substring(lineptr, 5));
            lineptr += 5;

            lineptr += 69;

            abc = Convert.ToDouble(line.Substring(lineptr, 18).Trim());
            T.A = abc;
            lineptr += 18;

            abc = Convert.ToDouble(line.Substring(lineptr, 14).Trim());
            T.B = abc;
            lineptr += 14;

            abc = Convert.ToDouble(line.Substring(lineptr, 20).Trim());
            T.C = abc;

            return T;
        }

    }


}
