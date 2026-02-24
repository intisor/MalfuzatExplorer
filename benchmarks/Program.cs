using BenchmarkDotNet.Running;
using MalfuzatExplorer.Benchmarks;

// Run with: dotnet run -c Release
BenchmarkRunner.Run<SearchBenchmarks>();
