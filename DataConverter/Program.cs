using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using VSOP87;


/// <summary>
/// Transform Source Data to binary format
/// </summary>

//List to Serialize
List<PlanetTable> VSOP87DATA = new List<PlanetTable>();

//All original data file location
DirectoryInfo dir = new DirectoryInfo(@"D:\VS Project\VSOP87DATA");

foreach (FileInfo file in dir.GetFiles())
{
    Console.WriteLine(file.FullName);
    VSOP87DATA.Add(ReadPlanet(file));
}

DirectoryInfo OutputDir = new DirectoryInfo(@"D:\");

DumpData(OutputDir, VSOP87DATA);


#region Verify Binary Data
PlanetTable[] VerifyVSOP87DATA;
using (FileStream fs = new FileStream(Path.Combine(OutputDir.FullName, "VSOP87DATA.BIN"), FileMode.Open))
{
    var bf = new BinaryFormatter();
    VerifyVSOP87DATA = (PlanetTable[])bf.Deserialize(fs);
    Console.WriteLine(Path.Combine(OutputDir.FullName, "VSOP87DATA.BIN") + Environment.NewLine + "Load OK");
    Console.WriteLine();
}

Calculator vsop = new Calculator();
PlanetTable planet = VSOP87DATA.Where(x => x.ibody == VSOPBody.MERCURY).Where(x => x.iver == VSOP87.VSOPVersion.VSOP87).First();

Stopwatch sw = Stopwatch.StartNew();
var results = vsop.GetPlanet(VSOPBody.MERCURY, VSOPVersion.VSOP87, new VSOPTime(DateTime.Now.ToUniversalTime()));
sw.Stop();

double ticks = sw.ElapsedTicks;
double Freq = Stopwatch.Frequency;
double milliseconds = (ticks / Freq) * 1000;
double microsecond = (ticks / Freq) * 1000000;
Console.WriteLine($"Elapsed milliseconds: {milliseconds} ms");
Console.WriteLine($"Elapsed microsecond: {microsecond} us");


#endregion

static PlanetTable ReadPlanet(FileInfo file)
{
    string[] Extensions = { @"sun", @"mer", @"ven", @"ear", @"mar", @"jup", @"sat", @"ura", @"nep", @"emb" };

    //parse filepath

    VSOP87.VSOPVersion iver = (VSOP87.VSOPVersion)Enum.Parse(typeof(VSOP87.VSOPVersion), file.Name.Split(".")[0]);
    VSOPBody ibody = (VSOPBody)Extensions.ToList().IndexOf(file.Name.Split(".")[1]);

    //create an empty dataset
    PlanetTable planetdata = new PlanetTable();
    planetdata.iver = iver;
    planetdata.ibody = ibody;
    planetdata.variables = new VariableTable[6];
    for (int ic = 0; ic < 6; ic++)
    {
        planetdata.variables[ic].PowerTables = new PowerTable[6];
    }


    Header H = new Header();
    string line;
    using (StreamReader sr = new StreamReader(file.FullName))
    {
        while ((line = sr.ReadLine()) != null)
        {
            ReadHeader(line, ref H);
            planetdata.variables[H.ic].PowerTables[H.it].header = new Header() { body = H.body, iv = H.iv, ib = H.ib, ic = H.ic, it = H.it, nt = H.nt };
            planetdata.variables[H.ic].PowerTables[H.it].Terms = new Term[H.nt];
            for (int i = 0; i < H.nt; i++)
            {
                line = sr.ReadLine();
                ReadTerm(line, ref planetdata.variables[H.ic].PowerTables[H.it].Terms[i]);
            }
        }
    }
    Console.WriteLine("Load OK");
    Console.WriteLine();

    return planetdata;
}

static void ReadHeader(string line, ref Header H)
{
    int lineptr = 17;
    H.iv = Convert.ToInt32(line.Substring(lineptr, 1).Trim());
    lineptr += 5;

    H.body = line.Substring(lineptr, 7).Trim();
    lineptr += 19;
    H.ib = (int)Enum.Parse(typeof(VSOPBody), H.body);
    H.ic = Convert.ToInt32(line.Substring(lineptr, 1).Trim()) - 1;
    lineptr += 18;

    H.it = Convert.ToInt32(line.Substring(lineptr, 1).Trim());
    lineptr += 1;

    H.nt = Convert.ToInt32(line.Substring(lineptr, 7).Trim());
}

static void ReadTerm(string line, ref Term T)
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
}


/// <summary>
/// Extremly unsafe, attacker can run code within the context
/// seek https://aka.ms/binaryformatter
/// </summary>
/// <param name="dir"></param>
static void DumpData(DirectoryInfo dir, List<PlanetTable> VSOP87DATA)
{
    string filename = Path.Combine(dir.FullName, "VSOP87DATA.BIN");
    if (File.Exists(filename))
    {
        File.Delete(filename);
    }

    PlanetTable[] buffer = VSOP87DATA.ToArray();

    using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate))
    {
        BinaryFormatter bf = new BinaryFormatter();
#pragma warning disable SYSLIB0011
        bf.Serialize(fs, buffer);
#pragma warning restore SYSLIB0011
    }

    Console.WriteLine(filename + Environment.NewLine + "Dump OK");
    Console.WriteLine();
}