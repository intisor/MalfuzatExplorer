using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace MalfuzatExplorer.Benchmarks;

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default
            .WithWarmupCount(2)
            .WithIterationCount(7)
            .WithUnrollFactor(1)
            .WithInvocationCount(1));

        AddColumn(StatisticColumn.Min);
        AddColumn(StatisticColumn.Max);
        AddColumn(StatisticColumn.Median);
        AddColumn(StatisticColumn.StdDev);
        AddColumn(RankColumn.Arabic);
        AddColumn(BaselineRatioColumn.RatioMean);

        AddLogger(ConsoleLogger.Default);
        AddExporter(MarkdownExporter.GitHub);
        WithOptions(ConfigOptions.DisableOptimizationsValidator);
    }
}
