namespace TomP2P.Benchmark
{
    public sealed class Arguments
    {
        private readonly string _bmArg;
        private readonly int _repetitions;
        private readonly string _resultsDir;
        private readonly int _warmupSec;
        private readonly string _suffix;

        public Arguments(string bmArg, int repetitions, string resultsDir, int warmupSec, string suffix)
        {
            _bmArg = bmArg;
            _repetitions = repetitions;
            _resultsDir = resultsDir;
            _warmupSec = warmupSec;
            _suffix = suffix;
        }

        public string BmArg
        {
            get { return _bmArg; }
        }

        public int Repetitions
        {
            get { return _repetitions; }
        }

        public string ResultsDir
        {
            get { return _resultsDir; }
        }

        public int WarmupSec
        {
            get { return _warmupSec; }
        }

        public string Suffix
        {
            get { return _suffix; }
        }
    }
}
