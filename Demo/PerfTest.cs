using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using VSOP87;

namespace Demo
{
    /// <summary>
    /// GetPlanetPosition(JUPITER, VSOP87D) for {Scalar, Vector256} x {Full, Fast (1"), UltraFast (10")}.
    /// Ratios are computed against the full series within the same SIMD mode.
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net10_0)]
    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
    public class PerfTest
    {
        private Calculator _full = null!;
        private Calculator _fast = null!;
        private Calculator _ultraFast = null!;

        private VSOPTime _vTime = null!;

        [Params(SimdMode.Scalar, SimdMode.Vector256)]
        public SimdMode Mode { get; set; }

        [GlobalSetup]
        public void Init()
        {
            _full = new Calculator(Mode, PrecisionMode.Full);
            _fast = new Calculator(Mode, PrecisionMode.Fast);
            _ultraFast = new Calculator(Mode, PrecisionMode.UltraFast);
            _vTime = new VSOPTime(DateTime.UtcNow, TimeFrame.UTC);
        }

        [Benchmark(Baseline = true)]
        public VSOPResult Full()
        {
            return _full.GetPlanetPosition(VSOPBody.JUPITER, VSOPVersion.VSOP87D, _vTime);
        }

        [Benchmark]
        public VSOPResult Fast()
        {
            return _fast.GetPlanetPosition(VSOPBody.JUPITER, VSOPVersion.VSOP87D, _vTime);
        }

        [Benchmark]
        public VSOPResult UltraFast()
        {
            return _ultraFast.GetPlanetPosition(VSOPBody.JUPITER, VSOPVersion.VSOP87D, _vTime);
        }
    }
}
