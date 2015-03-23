namespace TomP2P.Benchmark
{
    public sealed class Arguments
    {
        private readonly string _bmArg;
        private readonly int _nrRepetitions;
        private readonly string _resultsDir;
        private readonly int _nrWarmups;
        private readonly string _suffix;

        public Arguments(string bmArg, int nrWarmups, int nrRepetitions, string resultsDir, string suffix)
        {
            _bmArg = bmArg;
            _nrWarmups = nrWarmups;
            _nrRepetitions = nrRepetitions;
            _resultsDir = resultsDir;
            _suffix = suffix;
        }

        public string BmArg
        {
            get { return _bmArg; }
        }

        public int NrWarmups
        {
            get { return _nrWarmups; }
        }

        public int NrRepetitions
        {
            get { return _nrRepetitions; }
        }

        public string ResultsDir
        {
            get { return _resultsDir; }
        }

        public string Suffix
        {
            get { return _suffix; }
        }
    }
}
