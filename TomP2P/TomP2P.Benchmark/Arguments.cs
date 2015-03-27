using System.Text;

namespace TomP2P.Benchmark
{
    public sealed class Arguments
    {
        private readonly string _bmArg;
        private readonly string _type;
        private readonly int _nrWarmups;
        private readonly int _nrRepetitions;
        private readonly string _resultsDir;
        private readonly string _suffix;

        public Arguments(string bmArg, string type, int nrWarmups, int nrRepetitions, string resultsDir, string suffix)
        {
            _bmArg = bmArg;
            _type = type;
            _nrWarmups = nrWarmups;
            _nrRepetitions = nrRepetitions;
            _resultsDir = resultsDir;
            _suffix = suffix;
        }

        public string BmArg
        {
            get { return _bmArg; }
        }

        public string Type
        {
            get { return _type; }
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

        public override string ToString()
        {
            var sb = new StringBuilder("Arguments [bmArg = ")
                .Append(BmArg)
                .Append(", type = ").Append(Type)
                .Append(", nrWarmups = ").Append(NrWarmups)
                .Append(", nrRepetitions = ").Append(NrRepetitions)
                .Append(", resultsDir = ").Append(ResultsDir)
                .Append(", suffix = ").Append(Suffix);
            return sb.ToString();
        }
    }
}
