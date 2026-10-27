using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using VSOP87;

namespace Demo
{
    /// <summary>
    /// GetPlanetPosition(JUPITER, VSOP87D) for {Scalar, widest SIMD} x {Calculator (double), CalculatorF (float)}.
    /// Ratios are computed against the double calculator within the same SIMD mode.
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net10_0)]
    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
    public class PerfTest
    {
        private Calculator _double = null!;
        private CalculatorF _float = null!;

        private VSOPTime _vTime = null!;

        [ParamsSource(nameof(Modes))]
        public SimdMode Mode { get; set; }

        /// <summary>
        /// Scalar and the widest hardware-accelerated width of this machine.
        /// </summary>
        public static IEnumerable<SimdMode> Modes()
        {
            yield return SimdMode.Scalar;
            if (Vector512.IsHardwareAccelerated)
            {
                yield return SimdMode.Vector512;
            }
            else if (Vector256.IsHardwareAccelerated)
            {
                yield return SimdMode.Vector256;
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                yield return SimdMode.Vector128;
            }
        }

        [GlobalSetup]
        public void Init()
        {
            // Both calculators evaluate the full series.
            _double = new Calculator(Mode);
            _float = new CalculatorF(Mode);
            _vTime = new VSOPTime(DateTime.UtcNow, TimeFrame.UTC);
        }

        [Benchmark(Baseline = true)]
        public VSOPResult Double()
        {
            return _double.GetPlanetPosition(VSOPBody.JUPITER, VSOPVersion.VSOP87D, _vTime);
        }

        [Benchmark]
        public VSOPResult Float()
        {
            return _float.GetPlanetPosition(VSOPBody.JUPITER, VSOPVersion.VSOP87D, _vTime);
        }
    }
}
