// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace NCompileBench
{
    [BenchmarkCategory("Roslyn")]
    public class CompilationBenchmarks
    {
        private CSharpCompilation _comp;
        private MemoryStream _peStream;

        public void LoadCompilation()
        {
            _comp = Helpers.CreateReproCompilation();
        }

        [GlobalSetup(Targets = new[] {nameof(Compile) })]
        public void LoadCompilationAndGetDiagnostics()
        {
            LoadCompilation();
            _peStream = new MemoryStream();
            _ = _comp.GetDiagnostics();
        }

        [Params(true, false)]
        public bool IsConcurrent { get; set; }
        
        [Benchmark]
        public EmitResult Compile()
        {
            _peStream.Position = 0;
            var cSharpCompilationOptions = _comp.Options.WithConcurrentBuild(IsConcurrent);
            return _comp.WithOptions(cSharpCompilationOptions).Emit(_peStream);
        }
    }
}
