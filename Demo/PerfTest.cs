using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using VSOP87;

namespace Demo
{
    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    public class PerfTest
    {
        private Calculator vsop;

        private DateTime dt;
        private VSOPTime vTime;

        public PerfTest()
        { vsop = new Calculator(); }

        [GlobalSetup]
        public void init()
        {
            dt = DateTime.Now.ToUniversalTime();
            dt.ToUniversalTime();
            dt = dt.AddSeconds(-69.184);
            vTime = new VSOPTime(dt, TimeFrame.UTC);
        }

        [Benchmark]
        public VSOPResult Calculate()
        {
            var ell = vsop.GetPlanetPosition(VSOPBody.JUPITER, VSOPVersion.VSOP87D, vTime);
            return ell;
        }
    }
}