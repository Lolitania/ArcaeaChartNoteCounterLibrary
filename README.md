# Arcaea Chart Note Counter Library

A really simple [Arcaea](https://arcaea.lowiro.com/) chart note counter library written in C#.

Last updated for Arcaea v3.12.6.

## How to use

Simply call one of the following two methods:

<code>int Moe.Lowiro.Arcaea.Chart.CountNote(string path);</code>

<code>int Moe.Lowiro.Arcaea.Chart.CountNote(Stream stream);</code>

## Notes about compiling

Do **NOT** check "Code Optimization" in the Project Properties. It will affect floating point errors.