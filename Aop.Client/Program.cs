using Aop.Client.TSP;
using Aop.Client.Benchmarks;
using BenchmarkDotNet.Running;
using System.Diagnostics;

var tspFileReader = new TspFileReader("gr120.tsp");

if (tspFileReader is null)
    throw new Exception($"{nameof(tspFileReader)} is null");

Console.WriteLine($"Target: {tspFileReader.OptimalValue}");

var sw = new Stopwatch();
sw.Start();
var tsp = new Genetic(new(tspFileReader.ImMatrix));
var result = tsp.Run();
sw.Stop();
int proccesTime = Convert.ToInt32(sw.ElapsedMilliseconds);
sw.Reset();

Console.WriteLine($"BF: MinPath {(result.Cost - tspFileReader.OptimalValue) / (float)tspFileReader.OptimalValue * 100}" + "% " + $"Cost: {result.Cost} opt: {tspFileReader.OptimalValue} time: {proccesTime} ms");
