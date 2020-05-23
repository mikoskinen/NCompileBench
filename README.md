# NCompileBench 

How fast can your computer compile .NET code? Find out!

NCompileBench is a benchmark which compiles .NET Core 3.1 based C#-code. The benchmark runs the benchmark as concurrent and non-concurrent, but the NCompileBench Score is based on the concurrent result.

## Requirements

.NET Core 3.1 SDK is required in order to run the benchmark. You can download it from https://dotnet.microsoft.com/download/dotnet-core/3.1

x64 compatible Windows is required.

## Getting started

NCompileBench is available as dotnet global tool. To install:

```
dotnet tool install -g NCompileBench
```

After installing NCompileBench, it is available for running through the command line:

```
NCompileBench
```

Note that the benchmark starts automatically.

## Submitting results

The results "database" is available as a Gist: https://gist.github.com/mikoskinen/2560a85bc59ef6baad20d371ab0db6f2#file-ncompilebench-json-results

After running the benchmark, you can submit your score by running:

```
NCompileBench -submit
```

To just view the results without running the benchmark you can run:

```
NCompileBench -scores
```

## Acknowledgements

The source code is based on the [.NET Performance repository](https://github.com/dotnet/performance) by Microsoft (MIT-licensed).

The benchmark uses [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) (MIT-licensed).

The code compiled by this benchmark is the same one used in the .NET Performance repository, [CodeAnalysisReproWithAnalyzers.zip](https://github.com/dotnet/roslyn/releases/tag/perf-assets-v1)