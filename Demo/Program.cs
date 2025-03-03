using BenchmarkDotNet.Running;
using Demo;
using VSOP87;

Calculator vsop = new Calculator();

DateTime dt = DateTime.Now;
//string inputT = "2000-01-01T12:00:00.0000000Z";
//CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
//DateTimeStyles style = DateTimeStyles.AdjustToUniversal;
//DateTime.TryParse(inputT, culture, style, out dt);
//dt.ToUniversalTime();
//dt = dt.AddSeconds(-69.184);
VSOPTime vTime = new VSOPTime(dt, TimeFrame.UTC);
Console.WriteLine(vTime.JulianDate);

//var results = vsop.GetPlanetPosition(VSOPBody.EMB, VSOPVersion.VSOP87, vTime);
//FormattedPrint(results);

//var xyz = (results as VSOPResult_ELL).ToXYZ();
//FormattedPrint(xyz);
//xyz.ReferenceFrame = ReferenceFrame.ICRSJ2000;
//FormattedPrint(xyz);

//var lbr = xyz.ToLBR();
//FormattedPrint(lbr);
//lbr.ReferenceFrame = ReferenceFrame.DynamicalJ2000;
//FormattedPrint(lbr);

foreach (VSOPVersion iv in Enum.GetValues(typeof(VSOPVersion)))
{
    foreach (VSOPBody ib in Utility.ListAvailableBody(iv))
    {
        var results = vsop.GetPlanetPosition(ib, iv, vTime);
        FormattedPrint(results);
    }
}
var origin_XYZ = vsop.GetPlanetPosition(VSOPBody.EARTH, VSOPVersion.VSOP87C, vTime).Variables;

Console.WriteLine("origin XYZ Coordinates:");
Console.WriteLine($"X: {origin_XYZ[0]}");
Console.WriteLine($"Y: {origin_XYZ[1]}");
Console.WriteLine($"Z: {origin_XYZ[2]}");
Console.WriteLine($"dX: {origin_XYZ[3]}");
Console.WriteLine($"dY: {origin_XYZ[4]}");
Console.WriteLine($"dZ: {origin_XYZ[5]}");
Console.WriteLine();
var origin_LBR = vsop.GetPlanetPosition(VSOPBody.EARTH, VSOPVersion.VSOP87D, vTime).Variables;
Console.WriteLine("origin LBR Coordinates:");
Console.WriteLine($"L: {origin_LBR[0]}");
Console.WriteLine($"B: {origin_LBR[1]}");
Console.WriteLine($"R: {origin_LBR[2]}");
Console.WriteLine($"dL: {origin_LBR[3]}");
Console.WriteLine($"dB: {origin_LBR[4]}");
Console.WriteLine($"dR: {origin_LBR[5]}");
Console.WriteLine();

double[] converted_XYZ = Utility.LBRtoXYZ(origin_LBR);
Console.WriteLine("Converted XYZ Coordinates:");
Console.WriteLine($"X: {converted_XYZ[0]}");
Console.WriteLine($"Y: {converted_XYZ[1]}");
Console.WriteLine($"Z: {converted_XYZ[2]}");
Console.WriteLine($"dX: {converted_XYZ[3]}");
Console.WriteLine($"dY: {converted_XYZ[4]}");
Console.WriteLine($"dZ: {converted_XYZ[5]}");
Console.WriteLine();

double[] converted_LBR = Utility.XYZtoLBR(origin_XYZ);
Console.WriteLine("Converted LBR Coordinates:");
Console.WriteLine($"L: {converted_LBR[0]}");
Console.WriteLine($"B: {converted_LBR[1]}");
Console.WriteLine($"R: {converted_LBR[2]}");
Console.WriteLine($"dL: {converted_LBR[3]}");
Console.WriteLine($"dB: {converted_LBR[4]}");
Console.WriteLine($"dR: {converted_LBR[5]}");
Console.WriteLine();


Console.Write("Press Enter To Start Performance Test...");
Console.ReadLine();
#if DEBUG
var summary = BenchmarkRunner.Run<PerfTest>(new DebugBuildConfig());
#else
var summary = BenchmarkRunner.Run<PerfTest>();
#endif

Console.Write("Press Enter To Exit...");
Console.ReadLine();

void FormattedPrint(VSOPResult Result)
{
    Console.WriteLine("===============================================================");
    WriteColorLine(ConsoleColor.Blue, "PLANETARY EPHEMERIS VSOP87");
    Console.WriteLine("===============================================================");
    WriteColorLine("Version: ", ConsoleColor.Green, $"\t\t{Enum.GetName(Result.Version)} ");
    WriteColorLine("Body: ", ConsoleColor.Green, $"\t\t\t{Enum.GetName(Result.Body)}");
    switch (Result.CoordinatesType)
    {
        case CoordinatesType.Elliptic:
            WriteColorLine("Coordinates Type: ", ConsoleColor.Green, $"\tElliptic Elements");
            break;

        case CoordinatesType.Rectangular:
            WriteColorLine("Coordinates Type: ", ConsoleColor.Green, $"\tCartesian Coordinate");
            break;

        case CoordinatesType.Spherical:
            WriteColorLine("Coordinates Type: ", ConsoleColor.Green, $"\tSpherical Coordinate");
            break;
    }
    switch (Result.CoordinatesReference)
    {
        case CoordinatesReference.EclipticHeliocentric:
            WriteColorLine("Coordinates Reference: ", ConsoleColor.Green, $"\tEcliptic Heliocentric");
            break;

        case CoordinatesReference.EclipticBarycentric:
            WriteColorLine("Coordinates Reference: ", ConsoleColor.Green, $"\tEcliptic Barycentric");
            break;

        case CoordinatesReference.EquatorialHeliocentric:
            WriteColorLine("Coordinates Reference: ", ConsoleColor.Green, $"\tEquatorial Heliocentric");
            break;
    }
    switch (Result.ReferenceFrame)

    {
        case ReferenceFrame.DynamicalJ2000:
            WriteColorLine("Reference Frame: ", ConsoleColor.Green, $"\tDynamical equinox and ecliptic J2000");
            break;

        case ReferenceFrame.DynamicalDate:
            WriteColorLine("Reference Frame: ", ConsoleColor.Green, $"\tDynamical equinox and ecliptic of date");
            break;

        case ReferenceFrame.ICRSJ2000:
            WriteColorLine("Reference Frame: ", ConsoleColor.Green, $"\tICRS equinox and ecliptic J2000");
            break;
    }
    WriteColorLine("At UTC: ", ConsoleColor.Green, $"\t\t{Result.Time.UTC.ToUniversalTime().ToString("o")}");
    WriteColorLine("At TDB: ", ConsoleColor.Green, $"\t\t{Result.Time.TDB.ToString("o")}");

    if (Result is VSOPResult_ELL)
    {
        var ResultELL = ((VSOPResult_ELL)Result);
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

    if (Result is VSOPResult_XYZ)
    {
        var ResultXYZ = ((VSOPResult_XYZ)Result);
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

    if (Result is VSOPResult_LBR)
    {
        var ResultLBR = (VSOPResult_LBR)Result;
        Console.WriteLine("---------------------------------------------------------------");
        Console.WriteLine(String.Format("{0,-33}{1,30}", "longitude (rad)", ResultLBR.l));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "latitude (rad)", ResultLBR.b));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "radius (au)", ResultLBR.r));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "longitude velocity (rd/day)", ResultLBR.dl));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "latitude velocity (rd/day)", ResultLBR.db));
        Console.WriteLine(String.Format("{0,-33}{1,30}", "radius velocity (au/day)", ResultLBR.dr));
        Console.WriteLine("===============================================================");
        Console.WriteLine();
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