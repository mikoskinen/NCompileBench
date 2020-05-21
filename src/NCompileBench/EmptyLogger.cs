using BenchmarkDotNet.Loggers;

namespace NCompileBench
{
    public class EmptyLogger : ILogger
    {
        public void Write(LogKind logKind, string text)
        {
        
        }

        public void WriteLine()
        {
        }

        public void WriteLine(LogKind logKind, string text)
        {
        }

        public void Flush()
        {
        }

        public string Id { get; } = "EmptyLogger";
        public int Priority { get; } = 0;
    }
}