# VSOP87.NET 

## What's this?

VSOP was developed and is maintained (updated with the latest data) by the scientists at the Bureau des Longitudes in Paris. 

VSOP87, computed the positions of the planets directly at any moment, as well as their orbital elements with improved accuracy.

Original VSOP87 Solution was write by FORTRAN 77 . It's too old to use. 

Many Rewrite versions of VSOP87 is just a translation from FORTRAN. 

This is the best VSOP87 library ever.

The Hotspot of VSOP87 is located in 

``` (su, cu) = Math.SinCos(u); ```

Cost almost 90%+ CPU clocks.

I use default sincos function to ensure precision.

If anyone replace it with fast sincos funtion. this Algorithm will become much faster but less precision.

## Features

1. Use VSOPResult class to manage calculate results.
2. Use VSOPTime class to manage time. 
</br>Easy to convert time by calling ```VSOPTime.UTC```, ```VSOPTime.TAI```, ```VSOPTime.TDB```
3. Veryhigh performance per solution
![Performance Test](https://github.com/kingsznhone/VSOP87.NET/blob/master/PerformanceTest.png)
4. Useful Utility class. Such as checking planet available in specific version.
5. Async Api 
6. Use [MessagePack](https://github.com/neuecc/MessagePack-CSharp#lz4-compression"MessagePack for C#") for binary serialize.
<br>Initialization time becomes less than 10% of previous.
7. Brotli compression on source data. ~34Mb -> ~3MB with no precision lost.
<br>

## How to use

* NuGet Package Manager
    ```
    PM> NuGet\Install-Package VSOP87.NET -Version 1.1.6
    ```

```
using VSOP87;

Calculator vsop = new Calculator();

//Create VSOPTime using UTC .
DateTime Tinput = DateTime.Now;
VSOPTime vTime = new VSOPTime(Tinput.ToUniversalTime(),TimeFrame.UTC);

//Calculate Earth's present position with VSOP version D
var result=vsop.GetPlanetPosition(VSOPBody.EARTH, VSOPVersion.VSOP87D, vTime);

//VSOP version D Output LBR Coordinate.
//Neet cast to LBR result
VSOPResult_LBR Result_LBR = (VSOPResult_LBR)result;

//Print result

Console.WriteLine($"Version: {Enum.GetName(Result_LBR.Version)}");
Console.WriteLine($"Body: {Enum.GetName(Result_LBR.Body)}");
Console.WriteLine($"Coordinates Type: {Enum.GetName(Result_LBR.CoordinatesType)}");
Console.WriteLine($"Coordinates Reference: {Enum.GetName(Result_LBR.CoordinatesReference)}");
Console.WriteLine($"Reference Frame: {Enum.GetName(Result_LBR.ReferenceFrame)}");

Console.WriteLine($"Time UTC: {Result_LBR.Time.UTC.ToString("o")}");
Console.WriteLine($"Time TDB: {Result_LBR.Time.TDB.ToString("o")}");
Console.WriteLine("---------------------------------------------------------------");
Console.WriteLine(String.Format("{0,-33}{1,30}", "longitude (rad)", Result_LBR.l));
Console.WriteLine(String.Format("{0,-33}{1,30}", "latitude (rad)", Result_LBR.b));
Console.WriteLine(String.Format("{0,-33}{1,30}", "radius (au)", Result_LBR.r));
Console.WriteLine(String.Format("{0,-33}{1,30}", "longitude velocity (rd/day)", Result_LBR.dl));
Console.WriteLine(String.Format("{0,-33}{1,30}", "latitude velocity (rd/day)", Result_LBR.db));
Console.WriteLine(String.Format("{0,-33}{1,30}", "radius velocity (au/day)", Result_LBR.dr));
Console.WriteLine("===============================================================");

```

# Change Log

### V1.1.6 2023.07.07

Bug fix.

### V1.1.5 2023.07.06

Inpired from vsop2013, Add `dynamical equinox and ecliptic` to `ICRS frame` conversion.

Use MessagePack and brotli to compress original data.

Some bug fix. 

### V1.1.2 2023.07.05

Bug fix

Add ```VSOPTime.JulianDate``` property

Delete float version. It's useful and nosense.

Add ELL coord to XYZ coord conversion.

Add ELL coord to LBR coord conversion.

add XYZ coord to LBR coord conversion

add LBR coord to XYZ coord conversion

function that convert ELL to XYZ is copy from VSOP2013.

This is a magic function way beyond my math level.

So I can't find how to inverse XYZ elements to ELL elements.

Need help.

### V1.1.0 2023.07.02  
Bug fix 

Move to .NET7 for better performance.

Add async api

Add performance test framework 
 
<br>

### v1.0.3 2023.03.23

Add float version calculator ```VSOP87.CalculatorF```. 

But no performance Improvement.
 
<br>

### v1.0 2022.06.05

Code Cleaning.

Performance optimization.
 
<br>

### beta 2022.03.12

A lot of new features.

Performance Optimization.
 
<br>

### alpha 2021.12.04 

I make a data converter

Converting text data file into binary serialized file and embed in core DLL

All 6 version solution & All planets supported

Original data and solution download 

 ftp://ftp.imcce.fr/pub/ephem/planets/vsop87/

VSOP87 algorithm remastered in C#

<br>

# About VSOP87DATA.BIN

It's a RAM db that 80's would never imaging a computer can hold that huge amout data  

It's loaded into RAM when initiate VSOP calculator class

<br>

# Enviroment 

.NET 6 Runtime Windows x64

.NET 7 Runtime Windows x64

<br>

# API

## Class Calculator

This Class provide double precision results.

<br>

#### Methods

###  ```VSOPResult Calculator.GetPlanetPosition(VSOPBody ibody, VSOPVersion iver, VSOPTime time)```

Get Planet Position based on body, version, time.

Use ```Utility.AvailableBody(VSOPVersion ver)``` to check all available body in version.

<br>

#### Parameters

```ibody``` VSOPBody

Enum of all planet in VSOP87.

```iver``` VSOPVersion

Enum of all Version in VSOP87.

```time``` VSOPTime

A wrapper of ```Datetime```. Help converting between UTC & TDB.

<br>

#### Return

```VSOPResult``` 

abstract class contain 6 variables of planet position. 

should be explicit cast to ```VSOPResult_ELL```, ```VSOPResult_XYZ```, ```VSOPResult_LBR```.

<br/>

### ```async Task<VSOPResult> GetPlanetPositionAsync(VSOPBody ibody, VSOPVersion iver, VSOPTime time)```

Wrapper of ```GetPlanetPosition```,but async.

<br/>


## Class Utility

This Class Provide some useful function.

<br/>

#### Methods

### ```static List<VSOPBody> ListAvailableBody(VSOPVersion ver)```

#### Parameters

```ver``` VSOPVersion

Enum of Version in VSOP87.

<br>

#### Return

```List<VSOPBody>``` 

All Available planets in this version.

<br>

### ``` static bool CheckAvailability(VSOPVersion ver, VSOPBody body)```

#### Parameters

```ver``` VSOPVersion

Enum of Version in VSOP87.

```body``` VSOPVersion

Enum of body in VSOP87.

<br>

#### Return

```bool``` 

Is this version contain this body?

<br>

### ```static CoordinatesType GetCoordinatesType(VSOPVersion ver)```

#### Parameters

```ver``` VSOPVersion

Enum of Version in VSOP87.

<br>

#### Return

```CoordinatesType```

Coordinates type based on version. 

<br>

### ```static CoordinatesReference GetCoordinatesRefrence(VSOPVersion ver)```

#### Parameters

```ver``` VSOPVersion

Enum of Version in VSOP87.

<br>

#### Return

```CoordinatesReference```

Coordinates reference based on version. 

<br>

### ```static TimeFrameReference GetTimeFrameReference(VSOPVersion ver)```

#### Parameters

```ver``` VSOPVersion

Enum of Version in VSOP87.

<br>

#### Return

```TimeFrameReference```

Time frame reference based on version. 

<br>

### ```static double[,] MultiplyMatrix(double[,] A, double[,] B)```

A handy matrix multiply function

#### Parameters

```A``` double[,]

Matrix A

<br>

```B``` double[,]

Matrix B

<br>

#### Return

```double[,]```

Matrix C = AB.

<br>

### ```static double[] XYZtoLBR(double[] xyz)```

#### Parameters

```xyz``` double[]

Array of cartesian coordinate elements

<br>

#### Return

```double[]```

Array of spherical coordinate elements

<br>

### ```static double[] LBRtoXYZ(double[] lbr)```

#### Parameters

```lbr``` double[]

Array of spherical coordinate elements

<br>

#### Return

```double[]```

Array of cartesian coordinate elements

<br>

### ```static double[] ELLtoXYZ(double[] ell)```

This is a magic function I directly copy from VSOP2013.

It's way beyond my math level.

So I can't find how to inverse XYZ elements to ELL elements.

Need help.

#### Parameters

```ell``` double[]

Array of elliptic coordinate elements

<br>

#### Return

```double[]```

Array of cartesian coordinate elements

<br>

### ```static double[] ELLtoLBR(double[] ell)```

#### Parameters

```ell``` double[]

Array of elliptic coordinate elements

<br>

#### Return

```double[]```

Array of spherical coordinate elements

<br>

### ```static double[] DynamicaltoICRS(double[] xyz)```

#### Parameters

```xyz``` double[]

Array of cartesian coordinate elements that inertial frame of dynamical equinox and ecliptic.

<br>

#### Return

```double[]```

Array of cartesian coordinate elements that inertial frame of ICRS equinox and ecliptic.

<br>

### ```static double[] ICRStoDynamical(double[] xyz)```

#### Parameters

```xyz``` double[]

Array of cartesian coordinate elements that inertial frame of ICRS equinox and ecliptic.

<br>

#### Return

```double[]```

Array of cartesian coordinate elements that inertial frame of dynamical equinox and ecliptic.

<br>

## Class VSOPResult_XYZ : VSOPResult

#### Constructor

### ``` public VSOPResult_XYZ(VSOPVersion version, VSOPBody body, VSOPTime time, double[] variables)```

#### Arguments

```version``` VSOPVersion

version of this result from calculator.

<br>

```body``` VSOPBody

Planet

<br>

```time``` VSOPTime

Time wrapper for VSOP

<br>

```variables``` double[]

Raw Data from calculator.

<br>

### ```VSOPResult_XYZ(VSOPResult_LBR result)```

Create a new cartesian result from spherical result. 

<br>

#### Arguments

```result``` VSOPResult_LBR 

<br>

### ```VSOPResult_XYZ(VSOPResult_ELL result)```

Create a new Cartisian result from ellipitic result. 

<br>

#### Arguments

```result``` VSOPResult_ELL

<br>

### Properties

 ```VSOPVersion Version { get; }```

VSOP version of this result.

<br>

```VSOPBody Body { get; }```

Planet of this result.

<br>

```CoordinatesType CoordinatesType { get; }```

Coordinates type of this result.

<br>

```CoordinatesReference CoordinatesRefrence { get; }```

Coordinates reference of this result.

<br>

```ReferenceFrame ReferenceFrame { get; set;}```

ReferenceFrame of this result. Set to ```ReferenceFrame.ICRSJ2000``` or ```ReferenceFrame.DynamicalJ2000``` will automatically change coordinate field. 

'Dynamical frame of the date' is not supported.

'Barycentric Coordinates' is not supported.

'Elliptic Coordinates' is not supported.

<br>

```VSOPTime Time  { get; }```

Input time of this result.

<br>

```double[] Variables { get;}```

Raw data of this result.

<br>

```double x {get;}```

Position x (au)

```double y {get;}```

Position y (au)

```double z {get;}```

Position z (au)

```double dx {get;}```

Velocity x (au/day)

```double dy {get;}```

Velocity y (au/day)

```double dz {get;}```

Velocity z (au/day)

#### Methods

### ```VSOPResult_LBR ToLBR()```

Convert Result to Spherical coordinate.

<br>

## Class VSOPResult_ELL : VSOPResult

#### Constructor

### ```public VSOPResult_ELL((VSOPVersion version, VSOPBody body, VSOPTime time, double[] ell)```

Create a new spherical result from cartesian result. 

<br>

#### Arguments

```version``` VSOPVersion

version of this result from calculator.

<br>

```body``` VSOPBody

Planet 

<br>

```time``` VSOPTime

Time wrapper for VSOP

<br>

```ell``` double[]

Raw result data from calculator.

<br>

### Properties

```VSOPVersion Version { get; }```

VSOP version of this result.

<br>

```VSOPBody Body { get; }```

Planet of this result.

<br>

```CoordinatesType CoordinatesType { get; }```

Coordinates type of this result.

<br>

```CoordinatesReference CoordinatesRefrence { get; }```

Coordinates reference of this result.

<br>

```ReferenceFrame ReferenceFrame { get; }```

Reference frame of this result.

<br>

```VSOPTime Time  { get; }```

Input time of this result.

<br>

```double[] Variables { get;}```

Raw data of this result.

<br>

```double a {get;}```

Semi-major axis (au)

```double l {get;}```

Mean longitude (rd)

```double k {get;}```

e*cos(pi) (rd)

```double h {get;}```

e*sin(pi) (rd)

```double q {get;}```
sin(i/2)*cos(omega) (rd)

```double p {get;}```

sin(i/2)*sin(omega) (rd)

<br>

#### Methods

### ```VSOPResult_XYZ ToXYZ()```

Convert Result to Cartesian coordinate.

<br>

### ```VSOPResult_LBR ToLBR()```

Convert Result to Spherical coordinate.

<br>


## Class VSOPResult_LBR : VSOPResult

#### Constructor

### ``` public VSOPResult_LBR(VSOPVersion version, VSOPBody body, VSOPTime time, double[] variables)```

#### Arguments

```version``` VSOPVersion

version of this result from calculator.

<br>

```body``` VSOPBody

Planet

<br>

```time``` VSOPTime

Time wrapper for VSOP

<br>

```variables``` double[]

Raw Data from calculator.

<br>

### ```VSOPResult_LBR(VSOPResult_XYZ result)```

Create a new spherical result from cartesian result. 

<br>

#### Arguments

```result``` VSOPResult_XYZ 

<br>

### ```VSOPResult_XYZ(VSOPResult_ELL result)```

Create a new Spherical result from ellipitic result. 

<br>

#### Arguments

```result``` VSOPResult_ELL

<br>

### Properties

```VSOPVersion Version { get; }```

VSOP version of this result.

<br>

```VSOPBody Body { get; }```

Planet of this result.

<br>

```CoordinatesType CoordinatesType { get; }```

Coordinates type of this result.

<br>

```CoordinatesReference CoordinatesRefrence { get; }```

Coordinates reference of this result.

<br>


```ReferenceFrame ReferenceFrame { get; set;}```

ReferenceFrame of this result. Set to ```ReferenceFrame.ICRSJ2000``` or ```ReferenceFrame.DynamicalJ2000``` will automatically change coordinate field. 

'Dynamical frame of the date' is not supported.

'Barycentric Coordinates' is not supported.

'Elliptic Coordinates' is not supported.

<br>

```VSOPTime Time  { get; }```

Input time of this result.

<br>

```double[] Variables { get;}```

Raw data of this result.

<br>

```double l {get;}```

longitude (rd)

```double b {get;}```

latitude (rd)

```double r {get;}```

radius (au)

```double dl {get;}```

longitude velocity (rd/day)

```double db {get;}```

latitude velocity (rd/day)

```double dr {get;}```

radius velocity (au/day)

<br>

#### Methods

### ```VSOPResult_XYZ ToXYZ()```

Convert Result to Cartesian coordinate.

<br>

## Class VSOPTime

#### summary

This class provide time convert and management for VSOP87.

<br>

#### Constructor

### ```VSOPTime(DateTime dt, TimeFrame frame)```

Time to initialize VSOPTime.

<br>

#### Properties

```DateTime dt```

time struct

<br>

```DateTime TAI```

International Atomic Time

<br>

```DateTime TT```

Terrestrial Time (aka. TDT)

<br>

```DateTime TDB```

Barycentric Dynamical Time 

VSOP87 use this time frame.

```double JulianDate```

Get Julian Date from TDB.

<br>

#### Methods

### ```static DateTime ChangeFrame(DateTime dt, TimeFrame SourceFrame, TimeFrame TargetFrame)```

#### Parameters

```dt``` DateTime

A Datetime of any frame.

<br>

```SourceFrame``` TimeFrame

Time frame of ```dt```

<br>

```TargetFrame``` TimeFrame

Target time frame.

<br>

#### Return

```DateTime```

Datetime of target time Frame.

<br>

### ```static double ToJulianDate(DateTime dt)```

#### Parameters

```dt``` DateTime

Datetime to convert

<br>

#### Return

```double```

Julian date.

<br>

### ```static DateTime FromJulianDate(double JD)```

#### Parameters

```double```

Julian date to analyze.

<br>

#### Return

```dt``` DateTime

Datetime Class.

<br>


