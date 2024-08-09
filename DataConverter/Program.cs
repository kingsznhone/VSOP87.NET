using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using MemoryPack;
using FastLZMA2Net;
namespace VSOP87
{
    public class Program
    {
        public static void Main(string[] args)
        {
            /// <summary>
            /// Transform Source Data to binary format
            /// </summary>
            List<PlanetTable> VSOP87DATA = new List<PlanetTable>();

            string[] ResourceFiles = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            Stopwatch sw = new();
            sw.Restart();

            foreach (string file in ResourceFiles)
            {
                Console.WriteLine(file);
                VSOP87DATA.Add(ReadPlanet(file));
            }

            sw.Stop();
            double ticks = sw.ElapsedTicks;
            double Freq = Stopwatch.Frequency;
            double milliseconds = (ticks / Freq) * 1000;
            Console.WriteLine($"Data Read & Convert OK...Elapsed milliseconds: {milliseconds} ms");
            Console.WriteLine();

            //Dump
            sw.Restart();

            DirectoryInfo OutputDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            DumpData(OutputDir, VSOP87DATA);

            sw.Stop();
            ticks = sw.ElapsedTicks;
            milliseconds = (ticks / Freq) * 1000;
            Console.WriteLine($"Data Dumped OK. Elapsed: {milliseconds}ms");
            Console.WriteLine();

            //Reload to verify

            sw.Restart();

            LoadData(OutputDir);

            sw.Stop();
            ticks = sw.ElapsedTicks;
            milliseconds = (ticks / Freq) * 1000;
            Console.WriteLine($"Dump Data Reload Test OK. Elapsed: {milliseconds}ms");
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = OutputDir.FullName,
                UseShellExecute = true
            });
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

            return planetdata;
        }

        private static Header ReadHeader(string line)
        {
            ReadOnlySpan<char> lineSpan = line.AsSpan();
            Header H = new Header();

            int lineptr = 17;
            H.Version = (VSOPVersion)int.Parse(lineSpan[lineptr..(lineptr+1)].Trim());
            lineptr += 5;

            H.body = (VSOPBody)Enum.Parse(typeof(VSOPBody), lineSpan[lineptr..(lineptr+7)].Trim());
            lineptr += 19;

            H.ic = int.Parse(lineSpan[lineptr..(lineptr+ 1)].Trim()) - 1;
            lineptr += 18;

            H.it = int.Parse(lineSpan[lineptr..(lineptr+ 1)].Trim());
            lineptr += 1;

            H.nt = int.Parse(lineSpan[lineptr..(lineptr+7)].Trim());
            return H;
        }

        private static void ReadTerm(string line, ref Term T)
        {
            ReadOnlySpan<char> lineSpan = line.AsSpan();
            int lineptr;

            lineptr = 5;
            //T.rank = Convert.ToInt32(line.Substring(lineptr, 5));
            lineptr += 5 + 69;

            T.A = double.Parse(lineSpan[lineptr..(lineptr+ 18)].Trim());
            lineptr += 18;

            T.B = double.Parse(lineSpan[lineptr..(lineptr+ 14)].Trim());
            lineptr += 14;

            T.C = double.Parse(lineSpan[lineptr..(lineptr+ 20)].Trim());
        }

        private static void DumpData(DirectoryInfo dir, List<PlanetTable> VSOP87DATA)
        {
            string filename = Path.Combine(dir.FullName, "VSOP87DATA.BIN");
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            using FileStream fs = new(filename, FileMode.OpenOrCreate);
            Compressor compressor = new Compressor(0, 10) { HighCompressLevel = 10 };
            var data = compressor.Compress(MemoryPackSerializer.Serialize(VSOP87DATA));
            fs.Write(data);
            Console.WriteLine($"Data dump to {filename}");
        }

        private static List<PlanetTable> LoadData(DirectoryInfo dir)
        {
            string filename = Path.Combine(dir.FullName, "VSOP87DATA.BIN");
            Decompressor decompressor = new Decompressor();
            var data = decompressor.Decompress(File.ReadAllBytes(filename));
            List<PlanetTable> tables = MemoryPackSerializer.Deserialize<List<PlanetTable>>(data);
            if (tables is null) throw new Exception("Load Data Fail.");
            return tables;
        }
    }
}