// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using VSOP87;



Random random = new Random();
double[] array_double = new double[1024];
ref double ref_double = ref MemoryMarshal.GetReference<double>(array_double);
for (int i = 0; i < array_double.Length; i++)
{
    array_double[i] = random.NextDouble();
}
double sum = array_double.Sum();
double sum2;
Vector256<double> vsum = new Vector256<double>();
nuint vectorSize = (nuint)Vector256<double>.Count;
nuint Offset = 0;

double dv1 = 0;
double dsum = 0;
double diff = 0;
for (Offset = 0; Offset <(nuint) array_double.Length; Offset += vectorSize)
{
    var v1 = Vector256.LoadUnsafe(ref ref_double, Offset);
    dsum = Vector256.Sum(vsum);
    vsum += v1;
    dsum = Vector256.Sum(vsum) - dsum;
    dv1 = Vector256.Sum(v1);
    diff += dsum - dv1;
    //Debug.Assert(dsum == dv1);
}
sum2 = Vector256.Sum(vsum);

//Debug.Assert(sum2 == sum);


bool hwaccel = Vector256.IsHardwareAccelerated;

Calculator vsop = new Calculator();
CalculatorF vsopF = new CalculatorF();
//PlanetTable SelectedPlanet = vsop.VSOP87DATA.Where(x => x.ibody == SelectedBody).Where(x => x.iver == SelectedVersion).First();
DateTime Tinput = DateTime.Now;
string inputT = "2023-01-01T00:00:00.0000000Z";
CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
DateTimeStyles style = DateTimeStyles.AdjustToUniversal;
DateTime.TryParse(inputT, culture, style, out Tinput);
VSOPTime vTime = new VSOPTime(Tinput);

var results = vsop.GetPlanet(VSOPBody.MERCURY, VSOPVersion.VSOP87D, vTime);
FormattedPrint(results);

results = vsop.GetPlanet_SIMD(VSOPBody.MERCURY, VSOPVersion.VSOP87D, vTime);
FormattedPrint(results);

var summary = BenchmarkRunner.Run<Calculator>();
var summaryF = BenchmarkRunner.Run<CalculatorF>();
Console.ReadLine();

Stopwatch sw = new Stopwatch();

vsop.preset();
vsopF.preset();
double[] testResult;
float[] testResultF;

//sw.Restart();
//for (int i = 0; i < 500; i++)
//{
//    testResult = vsop.Test_Legacy();
//}
//sw.Stop();
//Console.WriteLine($"Legacy runtime: {sw.ElapsedMilliseconds} ms");

//sw.Restart();
//for (int i = 0; i < 500; i++)
//{
//    testResult = vsop.Test_SIMD();
//}
//sw.Stop();
//Console.WriteLine($"SIMD runtime: {sw.ElapsedMilliseconds} ms");

//sw.Restart();
//for (int i = 0; i < 500; i++)
//{
//    testResultF = vsopF.Test_Legacy();
//}
//sw.Stop();
//Console.WriteLine($"Legacy runtime: {sw.ElapsedMilliseconds} ms");


//sw.Restart();
//for (int i = 0; i < 500; i++)
//{
//    testResultF = vsopF.Test_SIMD();
//}
//sw.Stop();
//Console.WriteLine($"SIMDF runtime: {sw.ElapsedMilliseconds} ms");





//foreach (VSOPVersion iv in Enum.GetValues(typeof(VSOPVersion)))
//{
//    foreach (VSOPBody ib in Utility.AvailableBody(iv))
//    {
//        results = vsop.GetPlanet(ib, iv, vTime);
//        FormattedPrint(results);
//        results = vsop.GetPlanet_SIMD(ib, iv, vTime);
//        FormattedPrint(results);
//        Console.WriteLine();
//    }
//}

//Console.Write("Press Enter To Start 64bit Performance Test...");
//Console.ReadLine();
//PerformanceTest(10000);

//Console.Write("Press Enter To Start 32bit Performance Test...");
//Console.ReadLine();
//PerformanceTestF(10000);
////PerformanceTestSingle(10000);

Console.Write("Press Enter To Exit...");
Console.ReadLine();

void FormattedPrint(VSOPResult Result)
{
    if (Result.Version == VSOPVersion.VSOP87)
    {
        var ResultELL = ((VSOPResultELL)Result);
        Console.WriteLine();
        Console.WriteLine($"{Enum.GetName(Result.Version)} Elliptic Elements Of {Enum.GetName(Result.Body)}");
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
        Console.WriteLine($"{Enum.GetName(Result.Version)} Rectangular Coordinates Of {Enum.GetName(Result.Body)}");
        if (ResultXYZ.Version == VSOPVersion.VSOP87A)
        {
            Console.WriteLine("Heliocentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic J2000.");
        }
        if (ResultXYZ.Version == VSOPVersion.VSOP87C)
        {
            Console.WriteLine("Heliocentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic Of The Date.");
        }
        if (ResultXYZ.Version == VSOPVersion.VSOP87E)
        {
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
        Console.WriteLine($"{Enum.GetName(Result.Version)} Rectangular Coordinates Of {Enum.GetName(Result.Body)}");
        if (ResultLBR.Version == VSOPVersion.VSOP87B)
        {
            Console.WriteLine("Heliocentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic J2000.");
        }
        if (ResultLBR.Version == VSOPVersion.VSOP87D)
        {
            Console.WriteLine("Heliocentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic Of The Date.");
        }
        Console.WriteLine($"UTC: {ResultLBR.Time.UTC.ToString("o")}");
        Console.WriteLine("=====================================================================");
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "longitude (rd)", ResultLBR.L));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "latitude (rd)", ResultLBR.B));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "radius (au)", ResultLBR.R));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "longitude velocity (rd/day)", ResultLBR.dL));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "latitude velocity (rd/day)", ResultLBR.dB));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "radius velocity (au/day)", ResultLBR.dR));
    }
}

void FormattedPrintF(VSOPResultF Result)
{
    if (Result.Version == VSOPVersion.VSOP87)
    {
        var ResultELL = ((VSOPResultFELL)Result);
        Console.WriteLine();
        Console.WriteLine($"{Enum.GetName(Result.Version)} Elliptic Elements Of {Enum.GetName(Result.Body)}");
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
        var ResultXYZ = ((VSOPResultFXYZ)Result);
        Console.WriteLine();
        Console.WriteLine($"{Enum.GetName(Result.Version)} Rectangular Coordinates Of {Enum.GetName(Result.Body)}");
        if (ResultXYZ.Version == VSOPVersion.VSOP87A)
        {
            Console.WriteLine("Heliocentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic J2000.");
        }
        if (ResultXYZ.Version == VSOPVersion.VSOP87C)
        {
            Console.WriteLine("Heliocentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic Of The Date.");
        }
        if (ResultXYZ.Version == VSOPVersion.VSOP87E)
        {
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
        var ResultLBR = (VSOPResultFLBR)Result;
        Console.WriteLine();
        Console.WriteLine($"{Enum.GetName(Result.Version)} Rectangular Coordinates Of {Enum.GetName(Result.Body)}");
        if (ResultLBR.Version == VSOPVersion.VSOP87B)
        {
            Console.WriteLine("Heliocentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic J2000.");
        }
        if (ResultLBR.Version == VSOPVersion.VSOP87D)
        {
            Console.WriteLine("Heliocentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic Of The Date.");
        }
        Console.WriteLine($"UTC: {ResultLBR.Time.UTC.ToString("o")}");
        Console.WriteLine("=====================================================================");
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "longitude (rd)", ResultLBR.L));
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
    Console.WriteLine("=====================Start Performance Test 64bit=====================");
    Stopwatch sw = new Stopwatch();
    sw.Start();
    object lockobject = new object();
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
            lock (lockobject)
            {
                completedCycle++;
                if (completedCycle % 1000 == 0)
                {
                    Console.WriteLine($"Cycle: {completedCycle,-10}  {sw.Elapsed.TotalMilliseconds,10} ms");
                }
            }
        }
    });

    sw.Stop();
    Console.WriteLine($"Performance Test 64bit Finished.");
    Console.WriteLine($"Time Used: {sw.Elapsed.TotalMilliseconds,10} ms");
}

void PerformanceTestF(int cycle)
{
    Console.WriteLine("=====================Start Performance Test 32bit=====================");
    Stopwatch sw = new Stopwatch();
    sw.Start();
    object lockobject = new object();
    int completedCycle = 0;
    var result = Parallel.For(0, cycle, (i) =>
    {
        {
            foreach (VSOPVersion iv in Enum.GetValues(typeof(VSOPVersion)))
            {
                foreach (VSOPBody ib in Utility.AvailableBody(iv))
                {
                    vsopF.GetPlanet(ib, iv, vTime);
                    //Console.WriteLine($"Time Used: {vsop.TimeUsed.TotalMilliseconds} ms");
                    //FormattedPrint(results);
                }
            }
            lock (lockobject)
            {
                completedCycle++;
                if (completedCycle % 1000 == 0)
                {
                    Console.WriteLine($"Cycle: {completedCycle,-10}  {sw.Elapsed.TotalMilliseconds,10} ms");
                }
            }
        }
    });

    sw.Stop();
    Console.WriteLine($"Performance Test 32bit Finished.");
    Console.WriteLine($"Time Used: {sw.Elapsed.TotalMilliseconds,10} ms");
}