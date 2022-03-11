﻿// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Globalization;
using VSOP87;

Calculator vsop = new Calculator();
//PlanetTable SelectedPlanet = vsop.VSOP87DATA.Where(x => x.ibody == SelectedBody).Where(x => x.iver == SelectedVersion).First();

DateTime Tinput = DateTime.Now;
//string inputT = "2021-09-22T19:07:26.0000000Z";
//CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
//DateTimeStyles style = DateTimeStyles.AdjustToUniversal;
//DateTime.TryParse(inputT, culture, style, out Tinput);
VSOPTime vTime = new VSOPTime(Tinput);
foreach (VSOPVersion iv in Enum.GetValues(typeof(VSOPVersion)))
{
    foreach (VSOPBody ib in Utility.AvailableBody(iv))
    {
        var results = vsop.GetPlanet(ib, iv, vTime);
        FormattedPrint(results);
        Console.WriteLine($"Time Used: {vsop.TimeUsed.TotalMilliseconds} ms");
        Console.WriteLine();
    }
}

PerformanceTest(1000);


Console.Write("Press Enter To Exit...");
Console.ReadLine();


void FormattedPrint(VSOPResult Result)
{
    if (Result.Version == VSOPVersion.VSOP87)
    {
        var ResultELL = ((VSOPResultELL)Result);
        Console.WriteLine();
        Console.WriteLine($"VSOP87 Elliptic Elements Of {Enum.GetName(Result.Body)}");
        Console.WriteLine("Dynamical equinox and ecliptic J2000.");
        Console.WriteLine($"UTC: {Result.Time.UTC.ToString("o")}");
        Console.WriteLine("=====================================================================");
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "semi-major axis (au)", ResultELL.a));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "mean longitude (rd)", ResultELL.l));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "k = e*cos(pi) (rd)", ResultELL.k));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "h = e*sin(pi) (rd)", ResultELL.h));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "q = sin(i/2)*cos(omega) (rd)", ResultELL.q));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "p = sin(i/2)*sin(omega) (rd)", ResultELL.p));
        Console.WriteLine("e:     eccentricity");
        Console.WriteLine("pi:    perihelion longitude");
        Console.WriteLine("i:     inclination");
        Console.WriteLine("omega: ascending node longitude");

    }

    if (Result.Version == VSOPVersion.VSOP87A ||
        Result.Version == VSOPVersion.VSOP87C ||
        Result.Version == VSOPVersion.VSOP87E)
    {
        var ResultXYZ = ((VSOPResultXYZ)Result);
        Console.WriteLine();
        if (ResultXYZ.Version == VSOPVersion.VSOP87A)
        {
            Console.WriteLine($"VSOP87A Rectangular Coordinates Of {Enum.GetName(ResultXYZ.Body)}");
            Console.WriteLine("Heliocentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic J2000.");
        }
        if (ResultXYZ.Version == VSOPVersion.VSOP87C)
        {
            Console.WriteLine($"VSOP87C Rectangular Coordinates Of {Enum.GetName(ResultXYZ.Body)}");
            Console.WriteLine("Heliocentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic Of The Date.");
        }
        if (ResultXYZ.Version == VSOPVersion.VSOP87E)
        {
            Console.WriteLine($"VSOP87E Rectangular Coordinates Of {Enum.GetName(ResultXYZ.Body)}");
            Console.WriteLine("Barycentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic J2000.");
        }
        Console.WriteLine($"UTC: {Result.Time.UTC.ToString("o")}");
        Console.WriteLine("=====================================================================");
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "position x (au)", ResultXYZ.x));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "position y (au)", ResultXYZ.y));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "position z (au)", ResultXYZ.z));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "velocity x (au/day)", ResultXYZ.dx));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "velocity y (au/day)", ResultXYZ.dy));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "velocity z (au/day)", ResultXYZ.dz));
    }


    if (Result.Version == VSOPVersion.VSOP87B || Result.Version == VSOPVersion.VSOP87D)
    {
        var ResultLBR = (VSOPResultLBR)Result;
        Console.WriteLine();
        if (ResultLBR.Version == VSOPVersion.VSOP87B)
        {
            Console.WriteLine($"VSOP87B Spherical Coordinates Of {Enum.GetName(ResultLBR.Body)}");
            Console.WriteLine("Heliocentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic J2000.");
        }
        if (ResultLBR.Version == VSOPVersion.VSOP87D)
        {
            Console.WriteLine($"VSOP87D Spherical Coordinates Of {Enum.GetName(ResultLBR.Body)}");
            Console.WriteLine("Heliocentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic Of The Date.");
        }
        Console.WriteLine($"UTC: {ResultLBR.Time.UTC.ToString("o")}");
        Console.WriteLine("=====================================================================");
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "longitude (rd)",ResultLBR.L));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "latitude (rd)", ResultLBR.B));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "radius (au)", ResultLBR.R));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "longitude velocity (rd/day)", ResultLBR.dL));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "latitude velocity (rd/day)", ResultLBR.dB));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "radius velocity (au/day)", ResultLBR.dR));
    }
}

double ModuloCircle(double RAD)
{
    RAD -= Math.Floor(RAD / 2 / Math.PI) * 2 * Math.PI;
    if (RAD < 0)
        RAD += 2 * Math.PI;
    return RAD;
}


void PerformanceTest(int cycle)
{
    Console.WriteLine();
    Console.WriteLine("=====================Start Performance Test=====================");
    Stopwatch sw = new Stopwatch();
    sw.Start();
    int completedCycle = 0;
    var result = Parallel.For(0, cycle, (i) =>
    {
        {
            foreach (VSOPVersion iv in Enum.GetValues(typeof(VSOPVersion)))
            {
                foreach (VSOPBody ib in Utility.AvailableBody(iv))
                {
                    vsop.GetPlanet(ib, iv, vTime);
                    //Console.WriteLine($"Time Used: {vsop.TimeUsed.TotalMilliseconds} ms");
                    //FormattedPrint(results);
                }
            }

            completedCycle++;
            if (completedCycle % 1000 == 0)
            {
                Console.WriteLine($"Cycle: {completedCycle,-10}  { sw.Elapsed.TotalMilliseconds,10} ms");
            }
        }
    });

    sw.Stop();
    Console.WriteLine($"Cycle: {cycle,-10}  { sw.Elapsed.TotalMilliseconds,10} ms");
}

