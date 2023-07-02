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

<br>

## How to use

```
using VSOP87;

Calculator vsop = new Calculator();

//Create VSOPTime using UTC .
DateTime Tinput = DateTime.Now;
VSOPTime vTime = new VSOPTime(Tinput.ToUniversalTime());

//Calculate Earth's present position with VSOP version D
var result=vsop.GetPlanetPosition(VSOPBody.EARTH, VSOPVersion.VSOP87D, vTime);

//VSOP version D Output LBR Coordinate.
//Neet cast to LBR result
VSOPResult_LBR Result_LBR = (VSOPResult_LBR)result;

//Print result

Console.WriteLine($"Version: {Enum.GetName(Result_LBR.Version)}");
Console.WriteLine($"Coordinates Type: {Enum.GetName(Result_LBR.CoordinatesType)}");
Console.WriteLine($"Coordinates Reference: {Enum.GetName(Result_LBR.CoordinatesReference)}");
Console.WriteLine($"Time Frame Reference: {Enum.GetName(Result_LBR.TimeFrameReference)}");
Console.WriteLine($"Body: {Enum.GetName(Result_LBR.Body)}");
Console.WriteLine($"Time: {Result_LBR.Time.UTC.ToString("o")}");
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

### V1.1 2023.07.02  
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
.NET 7 Runtime Windows x64

<br>

# API

## Class Calculator

### Overview

This Class provide double precision results.

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

## Class CalculatorF

### Overview

This Class provide single precision results.

<br/>

###  ```VSOPResultF Calculator.GetPlanetPosition(VSOPBody ibody, VSOPVersion iver, VSOPTime time)```

Get Planet Position based on body, version, time.

Use ```Utility.AvailableBody(VSOPVersion ver)``` to check all available body in version.

<br/>

#### Parameters

```ibody``` VSOPBody

Enum of all planet in VSOP87.

```iver``` VSOPVersion

Enum of all Version in VSOP87.

```time``` VSOPTime

A wrapper of ```Datetime```. Help converting between UTC & TDB.

<br/>

#### Return

```VSOPResultF``` 

abstract class contain 6 variables of planet position. 

should be explicit cast to ```VSOPResultF_ELL```, ```VSOPResultF_XYZ```, ```VSOPResultF_LBR```.

<br/>

### ```async Task<VSOPResultF> GetPlanetPositionAsync(VSOPBody ibody, VSOPVersion iver, VSOPTime time)```

Wrapper of ```GetPlanetPosition```,but async.

<br/>


## Static Class Utility

### Overview

This Class Provide some useful function.

<br/>

### ```static List<VSOPBody> AvailableBody(VSOPVersion ver)```

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

### Class VSOPResult_XYZ : VSOPResult

### Properties

```VSOPVersion Version { get; }```

VSOP version of this result.

<br>

```VSOPBody Body { get; }```

Planet of this result.

<br>

```CoordinatesReference CoordinatesRefrence { get; }```

Coordinates reference of this result.

<br>

```CoordinatesType CoordinatesType { get; }```

Coordinates type of this result.

<br>

```TimeFrameReference TimeFrameReference { get; }```

Time frame reference  of this result.

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

<br>

### Class VSOPResult_ELL : VSOPResult

### Properties

```VSOPVersion Version { get; }```

VSOP version of this result.

<br>

```VSOPBody Body { get; }```

Planet of this result.

<br>

```CoordinatesReference CoordinatesRefrence { get; }```

Coordinates reference of this result.

<br>

```CoordinatesType CoordinatesType { get; }```

Coordinates type of this result.

<br>

```TimeFrameReference TimeFrameReference { get; }```

Time frame reference  of this result.

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


### Class VSOPResult_LBR : VSOPResult

### Properties

```VSOPVersion Version { get; }```

VSOP version of this result.

<br>

```VSOPBody Body { get; }```

Planet of this result.

<br>

```CoordinatesReference CoordinatesRefrence { get; }```

Coordinates reference of this result.

<br>

```CoordinatesType CoordinatesType { get; }```

Coordinates type of this result.

<br>

```TimeFrameReference TimeFrameReference { get; }```

Time frame reference  of this result.

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

### Class VSOPResultF_XYZ : VSOPResultF

Almost same as ```VSOPResult_XYZ```, float version

<br>

### Class VSOPResultF_ELL : VSOPResultF

Almost same as ```VSOPResult_ELL```, float version

<br>

### Class VSOPResultF_LBR : VSOPResultF

Almost same as ```VSOPResult_LBR```, float version

<br>


### Class VSOPTime
#### summary

This class provide time convert and management for VSOP87.

<br>

#### Constructor

```VSOPTime(DateTime UTC)```

Use UTC Time to initialize VSOPTime.

<br>

#### Properties

```DateTime UTC```

UTC Time frame.

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

<br>

### Methods

```DateTime ChangeFrame(DateTime dt, TimeFrame TargetFrame)```

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

```static double ToJulianDate(DateTime dt)```
#### Parameters

```dt``` DateTime

Datetime to convert

<br>

#### Return

```double```

Julian date.

<br>

```static DateTime FromJulianDate(double JD)```

#### Parameters

```double```

Julian date to analyze.

<br>

#### Return

```dt``` DateTime

Datetime Class.

<br>


