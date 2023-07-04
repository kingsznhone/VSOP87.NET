using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualBasic;
using VSOP87;

Calculator vsop = new Calculator();

DateTime Tinput = DateTime.Now;
//string inputT = "1099-12-19T12:00:00.0000000Z";
//CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
//DateTimeStyles style = DateTimeStyles.AdjustToUniversal;
//DateTime.TryParse(inputT, culture, style, out Tinput);
//Tinput.ToUniversalTime();

////Tinput = VSOPTime.FromJulianDate(2305445);
Tinput = Tinput.AddSeconds(-69.184);
VSOPTime vTime = new VSOPTime(Tinput);
DateTime TDB = vTime.TDB;


//var debug = Utility.DateToJD(2000, 01, 01, 12, 00, 00);

//Utility.JDtoDate(2451545d);

double Diff_max = 0;
foreach (VSOPBody ib in Utility.ListAvailableBody(VSOPVersion.VSOP87))
{
    if (ib == VSOPBody.EMB) continue;
    var result = vsop.GetPlanetPosition(ib, VSOPVersion.VSOP87, vTime).Variables;
    var resultA = vsop.GetPlanetPosition(ib, VSOPVersion.VSOP87A, vTime).Variables;
    var Trans = Utility.ELLtoXYZ(ib,result);
    for (int i = 0;i< resultA.Length; i++)
    {
        double diff = Math.Abs(Trans[i] - resultA[i]);
        double diff_pct = Math.Abs(diff / resultA[i]);
        Diff_max = Math.Max(Diff_max, diff_pct);
        Debug.Assert(diff_pct < Math.Pow(10, -2));
        Debug.Assert(Trans[i] * resultA[i] > 0);

        Console.WriteLine($"{ib}-{i} to XYZ\t\t\tDIFF = {diff}\tpct{diff_pct:p2}");
    }
}

foreach (VSOPBody ib in Utility.ListAvailableBody(VSOPVersion.VSOP87A))
{
    if (ib == VSOPBody.EMB) continue;
    var resultC = vsop.GetPlanetPosition(ib, VSOPVersion.VSOP87A, vTime).Variables;
    var resultD = vsop.GetPlanetPosition(ib, VSOPVersion.VSOP87B, vTime).Variables;
    var Trans = Utility.XYZtoLBR(resultC);

    for (int i = 0; i < resultD.Length; i++)
    {
        double diff = Math.Abs(Trans[i] - resultD[i]);
        double diff_pct = Math.Abs(diff / resultD[i]);
        Diff_max = Math.Max(Diff_max, diff_pct);
        Debug.Assert(diff_pct  < Math.Pow(10, -2));
        Debug.Assert(Trans[i] * resultD[i] > 0);
        Console.WriteLine($"{ib}-{i} to LBR\t\t\tDIFF = {diff}\tpct{diff_pct:p2}");
    }
}


foreach (VSOPVersion iv in Enum.GetValues(typeof(VSOPVersion)))
{
    foreach (VSOPBody ib in Utility.ListAvailableBody(iv))
    {
        var results = vsop.GetPlanetPosition(ib, iv, vTime);
        FormattedPrint(results);
    }
}

Console.ReadLine();
Console.Write("Press Enter To Exit...");
Console.ReadLine();

void FormattedPrint(VSOPResult Result)
{
    if (Result.Version == VSOPVersion.VSOP87)
    {
        var ResultELL = ((VSOPResult_ELL)Result);
        Console.WriteLine("===============================================================");
        WriteColorLine("Version: ", ConsoleColor.Green, $"\t\t{Enum.GetName(Result.Version)} ");
        WriteColorLine("Coordinates: ", ConsoleColor.Green, "\t\tHeliocentric Elliptic");
        WriteColorLine("Reference Frame: ", ConsoleColor.Green, "\tDynamical Equinox and Ecliptic J2000");
        WriteColorLine("Body: ", ConsoleColor.Green, $"\t\t\t{Enum.GetName(Result.Body)}");
        WriteColorLine("At UTC: ", ConsoleColor.Green, $"\t\t{Result.Time.UTC.ToUniversalTime().ToString("o")}");
        WriteColorLine("At TDB: ", ConsoleColor.Green, $"\t\t{Result.Time.TDB.ToString("o")}");
        Console.WriteLine("---------------------------------------------------------------");
        Console.WriteLine(String.Format("{0,-33}{1,30}", "semi-major axis (au)", ResultELL.a));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "mean longitude (rad)", ResultELL.l));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "k = e*cos(pi) (rad)", ResultELL.k));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "h = e*sin(pi) (rad)", ResultELL.h));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "q = sin(i/2)*cos(omega) (rad)", ResultELL.q));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "p = sin(i/2)*sin(omega) (rad)", ResultELL.p));
        Console.WriteLine("---------------------------------------------------------------");
        Console.WriteLine("e:     eccentricity");
        Console.WriteLine("pi:    perihelion longitude");
        Console.WriteLine("i:     inclination");
        Console.WriteLine("omega: ascending node longitude");
        Console.WriteLine("===============================================================");
        Console.WriteLine();
    }

    if (Result.Version == VSOPVersion.VSOP87A ||
        Result.Version == VSOPVersion.VSOP87C ||
        Result.Version == VSOPVersion.VSOP87E)
    {
        var ResultXYZ = ((VSOPResult_XYZ)Result);
        Console.WriteLine("===============================================================");
        WriteColorLine("Version: ", ConsoleColor.Green, $"\t\t{Enum.GetName(Result.Version)} ");
        if (ResultXYZ.Version == VSOPVersion.VSOP87A)
        {
            WriteColorLine("Coordinates: ", ConsoleColor.Green, "\t\tHeliocentric Rectangular");
            WriteColorLine("Reference Frame: ", ConsoleColor.Green, "\tDynamical Equinox and Ecliptic J2000");
        }
        if (ResultXYZ.Version == VSOPVersion.VSOP87C)
        {
            WriteColorLine("Coordinates: ", ConsoleColor.Green, "\t\tHeliocentric Rectangular");
            WriteColorLine("Reference Frame: ", ConsoleColor.Green, "\tDynamical Equinox and Ecliptic of date");
        }
        if (ResultXYZ.Version == VSOPVersion.VSOP87E)
        {
            WriteColorLine("Coordinates: ", ConsoleColor.Green, "\t\tBarycentric  Rectangular");
            WriteColorLine("Reference Frame: ", ConsoleColor.Green, "\tDynamical Equinox and Ecliptic J2000");
        }
        WriteColorLine("Body: ", ConsoleColor.Green, $"\t\t\t{Enum.GetName(Result.Body)}");
        WriteColorLine("At UTC: ", ConsoleColor.Green, $"\t\t{Result.Time.UTC.ToString("o")}");
        WriteColorLine("At TDB: ", ConsoleColor.Green, $"\t\t{Result.Time.TDB.ToString("o")}");

        Console.WriteLine("---------------------------------------------------------------");
        Console.WriteLine(String.Format("{0,-33}{1,30}", "position x (au)", ResultXYZ.x));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "position y (au)", ResultXYZ.y));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "position z (au)", ResultXYZ.z));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "velocity x (au/day)", ResultXYZ.dx));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "velocity y (au/day)", ResultXYZ.dy));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "velocity z (au/day)", ResultXYZ.dz));
        Console.WriteLine("---------------------------------------------------------------");
        Console.WriteLine("===============================================================");
        Console.WriteLine();
    }

    if (Result.Version == VSOPVersion.VSOP87B || Result.Version == VSOPVersion.VSOP87D)
    {
        var ResultLBR = (VSOPResult_LBR)Result;
        Console.WriteLine("===============================================================");
        WriteColorLine("Version: ", ConsoleColor.Green, $"\t\t{Enum.GetName(Result.Version)} ");
        if (ResultLBR.Version == VSOPVersion.VSOP87B)
        {
            WriteColorLine("Coordinates: ", ConsoleColor.Green, "\t\tHeliocentric Spherical");
            WriteColorLine("Reference Frame: ", ConsoleColor.Green, "\tDynamical Equinox and Ecliptic J2000");
        }
        if (ResultLBR.Version == VSOPVersion.VSOP87D)
        {
            WriteColorLine("Coordinates: ", ConsoleColor.Green, "\t\tHeliocentric Spherical");
            WriteColorLine("Reference Frame: ", ConsoleColor.Green, "\tDynamical Equinox and Ecliptic of date");
        }
        WriteColorLine("Body: ", ConsoleColor.Green, $"\t\t\t{Enum.GetName(Result.Body)}");
        WriteColorLine("At UTC: ", ConsoleColor.Green, $"\t\t{Result.Time.UTC.ToString("o")}");
        WriteColorLine("At TDB: ", ConsoleColor.Green, $"\t\t{Result.Time.TDB.ToString("o")}");
        Console.WriteLine("---------------------------------------------------------------");
        Console.WriteLine(String.Format("{0,-33}{1,30}", "longitude (rad)", ResultLBR.l));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "latitude (rad)", ResultLBR.b));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "radius (au)", ResultLBR.r));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "longitude velocity (rd/day)", ResultLBR.dl));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "latitude velocity (rd/day)", ResultLBR.db));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "radius velocity (au/day)", ResultLBR.dr));
        Console.WriteLine("===============================================================");
    }
}

void WriteColorLine(params object[] oo)
{
    foreach (var o in oo)
        if (o == null)
            Console.ResetColor();
        else if (o is ConsoleColor)
            Console.ForegroundColor = (ConsoleColor)o;
        else
            Console.Write(o.ToString());
    Console.WriteLine();
    Console.ResetColor();
}