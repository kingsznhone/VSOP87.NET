using System.IO.Compression;
using System.Runtime.Intrinsics;
using MemoryPack;

namespace VSOP87
{
    internal static class VSOPData
    {
        public const double A1000 = 365250.0d;

        public static readonly int VersionCount = Enum.GetValues<VSOPVersion>().Max(v => (int)v) + 1;

        public static readonly int BodyCount = Enum.GetValues<VSOPBody>().Max(b => (int)b) + 1;

        /// <summary>
        /// Loads the Brotli compressed MemoryPack data file shipped next to the assembly.
        /// </summary>
        public static List<PlanetTable> Load()
        {
            string dataFilePath = Path.Combine(AppContext.BaseDirectory, "VSOP87.BR");
            using FileStream fs = new(dataFilePath, FileMode.Open, FileAccess.Read);
            using BrotliStream bs = new(fs, CompressionMode.Decompress);
            using MemoryStream ms = new();
            bs.CopyTo(ms);
            return MemoryPackSerializer.Deserialize<List<PlanetTable>>(ms.ToArray())
                ?? throw new InvalidDataException("Failed to load VSOP87 data.");
        }

        public static SimdMode ResolveMode(SimdMode mode)
        {
            return mode switch
            {
                SimdMode.Auto => Vector512.IsHardwareAccelerated ? SimdMode.Vector512
                    : Vector256.IsHardwareAccelerated ? SimdMode.Vector256
                    : Vector128.IsHardwareAccelerated ? SimdMode.Vector128
                    : SimdMode.Scalar,
                SimdMode.Scalar => SimdMode.Scalar,
                SimdMode.Vector128 when Vector128.IsHardwareAccelerated => mode,
                SimdMode.Vector256 when Vector256.IsHardwareAccelerated => mode,
                SimdMode.Vector512 when Vector512.IsHardwareAccelerated => mode,
                SimdMode.Vector128 or SimdMode.Vector256 or SimdMode.Vector512 =>
                    throw new PlatformNotSupportedException($"{mode} is not hardware accelerated on this machine."),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
            };
        }

        /// <summary>
        /// Splits one power table: every term with C = 0 is constant (B is 0 or pi), so A·cos(B) is folded into the
        /// secular coefficient of <paramref name="power"/>, kept as an exact hi + lo pair (several C = 0 terms may share a power).
        /// Returns the periodic terms, in file order.
        /// </summary>
        public static Term[] SplitSecular(Term[] terms, int power, double[] secularHi, double[] secularLo)
        {
            List<Term> periodic = new(terms.Length);
            foreach (Term term in terms)
            {
                if (term.C == 0d)
                {
                    // TwoSum: hi + lo carries the exact sum.
                    double x = term.A * Math.Cos(term.B);
                    double hi = secularHi[power];
                    double s = hi + x;
                    double bp = s - hi;
                    secularLo[power] += (hi - (s - bp)) + (x - bp);
                    secularHi[power] = s;
                }
                else
                {
                    periodic.Add(term);
                }
            }
            return periodic.ToArray();
        }

        // TimeSpan.TicksPerDay * 365250 = 3.15576e17 is exact in double.
        private const double TicksPerMillennium = TimeSpan.TicksPerDay * A1000;

        // Semi-major axis a0 (au) indexed by VSOPBody, from VSOP87.f; scales the distance budgets.
        private static readonly double[] _semiMajorAxis = [0.01, 0.3871, 0.7233, 1.0, 1.5237, 5.2026, 9.5547, 19.2181, 30.1096, 1.0];

        /// <summary>
        /// Truncation tolerance in arcseconds; 0 keeps the full series.
        /// </summary>
        public static double ToleranceArcsec(PrecisionMode precision) => precision switch
        {
            PrecisionMode.Full => 0d,
            PrecisionMode.Fast => 1d,
            PrecisionMode.UltraFast => 10d,
            _ => throw new ArgumentOutOfRangeException(nameof(precision), precision, null),
        };

        /// <summary>
        /// Truncation budget of one variable: the tolerance in radians, times a0 for distances
        /// (a of the elliptic version, rectangular coordinates, radius r). The rate budget is the same value per day.
        /// </summary>
        public static double Budget(VSOPVersion version, VSOPBody body, int variable, double toleranceArcsec)
        {
            double radians = toleranceArcsec * Math.PI / 648000d;
            bool distance = version switch
            {
                VSOPVersion.VSOP87 => variable == 0,
                VSOPVersion.VSOP87B or VSOPVersion.VSOP87D => variable == 2,
                _ => true,
            };
            return distance ? radians * _semiMajorAxis[(int)body] : radians;
        }

        /// <summary>
        /// Strict truncation of the periodic terms of one variable (all powers together).
        /// Over |t| &lt;= 1 millennium a dropped term changes the value by at most |A| and the rate by at most
        /// (|A·C| + a·|A|) / 365250 per day, so the terms are dropped by increasing contribution while both sums stay within budget.
        /// </summary>
        /// <param name="periodic">Periodic terms per power of t.</param>
        /// <param name="budget">Maximum value error; the rate budget is the same value per day.</param>
        /// <param name="withRate">Whether the rate budget applies.</param>
        /// <returns>The kept terms per power, in file order.</returns>
        public static Term[][] Truncate(Term[][] periodic, double budget, bool withRate)
        {
            if (budget <= 0d)
            {
                return periodic;
            }

            List<(int Power, int Index, double Value, double Rate, double Key)> costs = [];
            for (int it = 0; it < periodic.Length; it++)
            {
                for (int i = 0; i < periodic[it].Length; i++)
                {
                    Term term = periodic[it][i];
                    double value = Math.Abs(term.A);
                    double rate = (Math.Abs(term.A * term.C) + it * Math.Abs(term.A)) / A1000;
                    costs.Add((it, i, value, rate, withRate ? Math.Max(value, rate) : value));
                }
            }
            costs.Sort((x, y) => x.Key.CompareTo(y.Key));

            bool[][] dropped = Array.ConvertAll(periodic, p => new bool[p.Length]);
            double valueSum = 0d;
            double rateSum = 0d;
            foreach ((int power, int index, double value, double rate, _) in costs)
            {
                if (valueSum + value > budget || (withRate && rateSum + rate > budget))
                {
                    break;
                }
                valueSum += value;
                rateSum += rate;
                dropped[power][index] = true;
            }

            Term[][] kept = new Term[periodic.Length][];
            for (int it = 0; it < periodic.Length; it++)
            {
                kept[it] = periodic[it].Where((_, i) => !dropped[it][i]).ToArray();
            }
            return kept;
        }

        // Low part of 2pi: Math.Tau + TauLo carries ~107 bits.
        private const double TauLo = 2.4492935982947064e-16;

        /// <summary>
        /// Julian millennia since J2000 (TDB), plus the exact rounding error of the conversion.
        /// </summary>
        public static double Millennia(VSOPTime time, out double error)
        {
            long ticks = time.TDB.Ticks - VSOPTime.J2000Ticks;
            // |ticks| reaches ~3e17 per millennium, beyond 2^53: split into an exact hi + lo pair.
            double hi = ticks;
            double lo = ticks - (long)hi;
            double t = hi / TicksPerMillennium;
            // The remainder of a correctly rounded division is exact with FMA; both terms are small integers or below.
            error = (Math.FusedMultiplyAdd(-t, TicksPerMillennium, hi) + lo) / TicksPerMillennium;
            return t;
        }

        /// <summary>
        /// Evaluates the secular polynomial sum (hi[a] + lo[a])·t^a and its derivative (per millennium).
        /// For the angular variable the linear term (mean motion, up to ~3e4 rad) is reduced modulo 2pi
        /// without losing its low bits, so the result stays small.
        /// </summary>
        public static (double Value, double Rate) Secular(double[] hi, double[] lo, double t, double tError, bool reduce)
        {
            double value = 0d;
            double rate = 0d;
            // tPrev = t^(it-1), tPow = t^it
            double tPrev = 0d;
            double tPow = 1d;
            for (int it = 0; it < hi.Length; it++, tPrev = tPow, tPow *= t)
            {
                if (hi[it] == 0d && lo[it] == 0d)
                {
                    continue;
                }
                double s = hi[it] + lo[it];
                value += reduce && it == 1 ? LinearModTau(hi[it], t, tError) + lo[it] * t : s * tPow;
                rate += it * s * tPrev;
            }
            return (value, rate);
        }

        /// <summary>
        /// (n · (t + tError)) mod 2pi, in (-pi, pi], keeping the low bits of the product.
        /// </summary>
        private static double LinearModTau(double n, double t, double tError)
        {
            double p = n * t;
            double e = Math.FusedMultiplyAdd(n, t, -p) + n * tError;
            double k = Math.Round(p / Math.Tau);
            double r = Math.FusedMultiplyAdd(-k, Math.Tau, p);
            r = Math.FusedMultiplyAdd(-k, TauLo, r);
            return r + e;
        }

        public static double NormalizeAngle(double angle) => (angle % Math.Tau + Math.Tau) % Math.Tau;

        public static VSOPResult CreateResult(VSOPBody body, VSOPVersion version, VSOPTime time, double[] variables)
        {
            return version switch
            {
                VSOPVersion.VSOP87 => new VSOPResult_ELL(body, time, variables, FrameType.Dynamical, Epoch.J2000),
                VSOPVersion.VSOP87A => new VSOPResult_XYZ(body, time, variables, FrameType.Dynamical, Epoch.J2000),
                VSOPVersion.VSOP87B => new VSOPResult_LBR(body, time, variables, FrameType.Dynamical, Epoch.J2000),
                VSOPVersion.VSOP87C => new VSOPResult_XYZ(body, time, variables, FrameType.Dynamical, Epoch.OfDate),
                VSOPVersion.VSOP87D => new VSOPResult_LBR(body, time, variables, FrameType.Dynamical, Epoch.OfDate),
                VSOPVersion.VSOP87E => new VSOPResult_XYZ(body, time, variables, FrameType.Barycentric, Epoch.J2000),
                _ => throw new ArgumentOutOfRangeException(nameof(version), version, null),
            };
        }

        /// <summary>
        /// Index of the angular variable that is reduced to [0, 2pi), or -1.
        /// </summary>
        public static int AngleIndex(VSOPVersion version) => version switch
        {
            VSOPVersion.VSOP87 => 1,
            VSOPVersion.VSOP87B or VSOPVersion.VSOP87D => 0,
            _ => -1,
        };
    }
}
