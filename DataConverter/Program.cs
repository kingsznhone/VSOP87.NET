using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace VSOP87
{
    public class Program
    {
        public static void Main(string[] args)
        {
            /// <summary>
            /// Transform Source Data to binary format
            /// </summary>

            //List to Serialize
            List<PlanetTable> VSOP87DATA = new List<PlanetTable>();
            List<PlanetTableF> VSOP87DATAF = new List<PlanetTableF>();

            string[] ResourceFiles = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            foreach (string file in ResourceFiles)
            {
                Console.WriteLine(file);
                VSOP87DATA.Add(ReadPlanet(file));
                VSOP87DATAF.Add(ReadPlanetF(file));
            }

            //Dump

            DirectoryInfo OutputDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            DumpData(OutputDir, VSOP87DATA);
            DumpDataF(OutputDir, VSOP87DATAF);

            Console.ReadLine();
        }

        private static PlanetTable ReadPlanet(string file)
        {
            string[] Extensions = { @"sun", @"mer", @"ven", @"ear", @"mar", @"jup", @"sat", @"ura", @"nep", @"emb" };

            //parse filepath

            VSOPVersion iver = (VSOPVersion)Enum.Parse(typeof(VSOPVersion), file.Split(".")[2]);
            VSOPBody ibody = (VSOPBody)Extensions.ToList().IndexOf(file.Split(".")[3]);

            //create an empty dataset
            PlanetTable planetdata = new PlanetTable();
            planetdata.version = iver;
            planetdata.body = ibody;
            planetdata.variables = new VariableTable[6];
            for (int ic = 0; ic < 6; ic++)
            {
                planetdata.variables[ic].PowerTables = new PowerTable[6];
            }

            Header H;
            string line;
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(file))
            {
                using (StreamReader sr = new StreamReader(s))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        H = ReadHeader(line);
                        planetdata.variables[H.ic].version = H.Version;
                        planetdata.variables[H.ic].body = H.body;
                        planetdata.variables[H.ic].ic = H.ic;

                        planetdata.variables[H.ic].PowerTables[H.it].version = H.Version;
                        planetdata.variables[H.ic].PowerTables[H.it].body = H.body;
                        planetdata.variables[H.ic].PowerTables[H.it].ic = H.ic;
                        planetdata.variables[H.ic].PowerTables[H.it].it = H.it;
                        planetdata.variables[H.ic].PowerTables[H.it].header = H;
                        planetdata.variables[H.ic].PowerTables[H.it].Terms = new Term[H.nt];

                        for (int i = 0; i < H.nt; i++)
                        {
                            line = sr.ReadLine();
                            ReadTerm(line, ref planetdata.variables[H.ic].PowerTables[H.it].Terms[i]);
                        }
                    }
                }
            }
            Console.WriteLine("Load OK");
            Console.WriteLine();

            return planetdata;
        }

        private static PlanetTableF ReadPlanetF(string file)
        {
            string[] Extensions = { @"sun", @"mer", @"ven", @"ear", @"mar", @"jup", @"sat", @"ura", @"nep", @"emb" };

            //parse filepath

            VSOPVersion iver = (VSOPVersion)Enum.Parse(typeof(VSOPVersion), file.Split(".")[2]);
            VSOPBody ibody = (VSOPBody)Extensions.ToList().IndexOf(file.Split(".")[3]);

            //create an empty dataset
            PlanetTableF planetdata = new PlanetTableF();
            planetdata.version = iver;
            planetdata.body = ibody;
            planetdata.variables = new VariableTableF[6];
            for (int ic = 0; ic < 6; ic++)
            {
                planetdata.variables[ic].PowerTables = new PowerTableF[6];
            }

            Header H;
            string line;
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(file))
            {
                using (StreamReader sr = new StreamReader(s))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        H = ReadHeader(line);
                        planetdata.variables[H.ic].version = H.Version;
                        planetdata.variables[H.ic].body = H.body;
                        planetdata.variables[H.ic].ic = H.ic;

                        planetdata.variables[H.ic].PowerTables[H.it].version = H.Version;
                        planetdata.variables[H.ic].PowerTables[H.it].body = H.body;
                        planetdata.variables[H.ic].PowerTables[H.it].ic = H.ic;
                        planetdata.variables[H.ic].PowerTables[H.it].it = H.it;
                        planetdata.variables[H.ic].PowerTables[H.it].header = H;
                        planetdata.variables[H.ic].PowerTables[H.it].Terms = new TermF[H.nt];

                        for (int i = 0; i < H.nt; i++)
                        {
                            line = sr.ReadLine();
                            ReadTermF(line, ref planetdata.variables[H.ic].PowerTables[H.it].Terms[i]);
                        }
                    }
                }
            }
            Console.WriteLine("Load OK");
            Console.WriteLine();

            return planetdata;
        }

        private static Header ReadHeader(string line)
        {
            Header H = new Header();

            int lineptr = 17;
            H.Version = (VSOPVersion)Convert.ToInt32(line.Substring(lineptr, 1).Trim());
            lineptr += 5;

            H.body = (VSOPBody)Enum.Parse(typeof(VSOPBody), line.Substring(lineptr, 7).Trim());
            lineptr += 19;

            H.ic = Convert.ToInt32(line.Substring(lineptr, 1).Trim()) - 1;
            lineptr += 18;

            H.it = Convert.ToInt32(line.Substring(lineptr, 1).Trim());
            lineptr += 1;

            H.nt = Convert.ToInt32(line.Substring(lineptr, 7).Trim());
            return H;
        }

        private static void ReadTerm(string line, ref Term T)
        {
            int lineptr;

            lineptr = 5;
            T.rank = Convert.ToInt32(line.Substring(lineptr, 5));
            lineptr += 5 + 69;

            T.A = Convert.ToDouble(line.Substring(lineptr, 18).Trim());
            lineptr += 18;

            T.B = Convert.ToDouble(line.Substring(lineptr, 14).Trim());
            lineptr += 14;

            T.C = Convert.ToDouble(line.Substring(lineptr, 20).Trim());
        }

        private static void ReadTermF(string line, ref TermF T)
        {
            int lineptr;

            lineptr = 5;
            T.rank = Convert.ToInt32(line.Substring(lineptr, 5));
            lineptr += 5 + 69;

            T.A = Convert.ToSingle(line.Substring(lineptr, 18).Trim());
            lineptr += 18;

            T.B = Convert.ToSingle(line.Substring(lineptr, 14).Trim());
            lineptr += 14;

            T.C = Convert.ToSingle(line.Substring(lineptr, 20).Trim());
        }

        private static void DumpData(DirectoryInfo dir, List<PlanetTable> VSOP87DATA)
        {
            string filename = Path.Combine(dir.FullName, "VSOP87DATA.BIN");
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate))
            {
                BinaryFormatter bf = new BinaryFormatter();
#pragma warning disable SYSLIB0011 
                bf.Serialize(fs, VSOP87DATA);
#pragma warning restore SYSLIB0011 
            }

            Console.WriteLine(filename + Environment.NewLine + "Dump OK");
            Console.WriteLine();
        }

        private static void DumpDataF(DirectoryInfo dir, List<PlanetTableF> VSOP87DATA)
        {
            string filename = Path.Combine(dir.FullName, "VSOP87DATAF.BIN");
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate))
            {
                BinaryFormatter bf = new BinaryFormatter();
#pragma warning disable SYSLIB0011 
                bf.Serialize(fs, VSOP87DATA);
#pragma warning restore SYSLIB0011 
            }

            Console.WriteLine(filename + Environment.NewLine + "Dump OK");
            Console.WriteLine();
        }
    }
}