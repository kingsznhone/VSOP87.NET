# VSOP87.NET 

## What's this?

VSOP was developed and is maintained (updated with the latest data) by the scientists at the Bureau des Longitudes in Paris. 

VSOP87, computed the positions of the planets directly at any moment, as well as their orbital elements with improved accuracy.

Original VSOP87 Solution was write by FORTRAN 77 . It's too old to use. 

Many Rewrite versions of VSOP87 is just a translation from FORTRAN. 

I did a little bit more than translation.

The Hotspot of VSOP87 is located in 

``` (su, cu) = Math.SinCos(u); ```

it cost almost 90%+ CPU clocks.

I use default sincos function to ensure precision.

If replacing with fast sincos funtion. this Algorithm will become much faster but less precision.

## Features

1. Use VSOPResult class to manage calculate results.
2. Use VSOPTime class to manage time. 
</br>Easy to convert time by calling ```VSOPTime.UTC``` ```VSOPTime.TAI``` ```VSOPTime.TDB```
3. Veryhigh performance. About 0.5ms / Result on 5900HX
![Performance Test](https://github.com/kingsznhone/VSOP87.NET/blob/master/PerformanceTest.png)
4. A little bit Utility. Such as check wether a planet in such version.


## Update Log

### 2022.06.05

Code Cleaning.

Performance optimization.

### 2022.03.12

A lot of new features.

Performance Optimization.


### 2021.12.04 

I make a data converter

Converting text data file into binary serialized file and embed in core DLL

All 6 version solution & All planets supported

Original data and solution download 

 ftp://ftp.imcce.fr/pub/ephem/planets/vsop87/

VSOP87 algorithm remastered in C#

## About VSOP87DATA.BIN

It's a RAM db that 80's would never imaging a computer can hold that huge amout data  

It's loaded into RAM when initiate VSOP calculator class

# Enviroment 
.NET 6 Runtime
