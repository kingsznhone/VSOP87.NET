// See https://aka.ms/new-console-template for more information
using System.Globalization;
using VSOP87;

Calculator vsop = new Calculator();
VSOPVersion SelectedVersion = VSOPVersion.VSOP87D;
VSOPBody SelectedBody = VSOPBody.EARTH;
PlanetTable SelectedPlanet = vsop.VSOP87DATA.Where(x => x.ibody == SelectedBody).Where(x => x.iver == SelectedVersion).First();

DateTime TDB;
string inputT = "2021-09-22T19:07:26.0000000Z";
CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
DateTimeStyles style = DateTimeStyles.AdjustToUniversal;
DateTime.TryParse(inputT, culture, style, out TDB);

double[] results = vsop.CalcPlanet(ref SelectedPlanet, TDB);
FormattedPrint(results, SelectedBody, SelectedVersion, TDB);

Console.ReadLine();


void FormattedPrint(double[] Result,VSOPBody body, VSOP87.VSOPVersion ver,DateTime TDB)
{
    if (ver == VSOP87.VSOPVersion.VSOP87)
    {
        Console.WriteLine($"VSOP87 Elliptic Elements Of {Enum.GetName(body)}");
        Console.WriteLine("Dynamical equinox and ecliptic J2000.");
        Console.WriteLine($"UTC: {TimeConverter.TDBtoUTC(TDB).ToString("o")}");
        Console.WriteLine("=====================================================================");
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "semi-major axis (au)", Result[0]));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "mean longitude (rd)", Result[1]));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "k = e*cos(pi) (rd)", Result[2]));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "h = e*sin(pi) (rd)", Result[3]));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "q = sin(i/2)*cos(omega) (rd)", Result[4]));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "p = sin(i/2)*sin(omega) (rd)", Result[5]));
        Console.WriteLine("e:     eccentricity");
        Console.WriteLine("pi:    perihelion longitude");
        Console.WriteLine("i:     inclination");
        Console.WriteLine("omega: ascending node longitude");
        Console.WriteLine();
    }

    if (ver == VSOP87.VSOPVersion.VSOP87A|| ver == VSOP87.VSOPVersion.VSOP87C|| ver == VSOP87.VSOPVersion.VSOP87E)
    {
        if(ver == VSOP87.VSOPVersion.VSOP87A)
        {
            Console.WriteLine($"VSOP87A Rectangular Coordinates Of {Enum.GetName(body)}");
            Console.WriteLine("Heliocentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic J2000.");
        }
        if (ver == VSOP87.VSOPVersion.VSOP87C)
        {
            Console.WriteLine($"VSOP87C Rectangular Coordinates Of {Enum.GetName(body)}");
            Console.WriteLine("Heliocentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic Of The Date.");
        }
        if (ver == VSOP87.VSOPVersion.VSOP87E)
        {
            Console.WriteLine($"VSOP87E Rectangular Coordinates Of {Enum.GetName(body)}");
            Console.WriteLine("Barycentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic J2000.");
        }
        Console.WriteLine($"UTC: {TimeConverter.TDBtoUTC(TDB).ToString("o")}");
        Console.WriteLine("=====================================================================");
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "position x (au)", Result[0]));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "position y (au)", Result[1]));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "position z (au)", Result[2]));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "velocity x (au/day)", Result[3]));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "velocity y (au/day)", Result[4]));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "velocity z (au/day)", Result[5]));
        Console.WriteLine();
    }


    if (ver == VSOP87.VSOPVersion.VSOP87B||ver == VSOP87.VSOPVersion.VSOP87D)
    {
        if (ver == VSOP87.VSOPVersion.VSOP87B)
        {
            Console.WriteLine($"VSOP87B Spherical Coordinates Of {Enum.GetName(body)}");
            Console.WriteLine("Heliocentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic J2000.");
        }
        if (ver == VSOP87.VSOPVersion.VSOP87D)
        {
            Console.WriteLine($"VSOP87D Spherical Coordinates Of {Enum.GetName(body)}");
            Console.WriteLine("Heliocentric Positions And Velocities");
            Console.WriteLine("Dynamical Equinox And Ecliptic Of The Date.");
        }
        Console.WriteLine($"UTC: {TimeConverter.TDBtoUTC(TDB).ToString("o")}");
        Console.WriteLine("=====================================================================");
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "longitude (rd)", ModuloCircle(Result[0])));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "latitude (rd)", Result[1]));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "radius (au)", Result[2]));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "longitude velocity (rd/day)", Result[3]));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "latitude velocity (rd/day)", Result[4]));
        Console.WriteLine(String.Format("{0,-30} : {1,30}", "radius velocity (au/day)", Result[5]));
        Console.WriteLine();
    }
}

double ModuloCircle(double RAD)
{
    RAD -= Math.Floor(RAD / 2/Math.PI)*2*Math.PI;
    if (RAD < 0)
        RAD+= 2*Math.PI;
    return RAD;
}


