using BenchmarkDotNet.Running;
using MalfuzatExplorer.Benchmarks;

// Run with: dotnet run -c Release --project benchmarks
// Pass a number to skip the menu, e.g.: dotnet run -c Release --project benchmarks -- 0
BenchmarkSwitcher.FromAssembly(typeof(SearchBenchmarks).Assembly).Run(args);
