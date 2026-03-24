using System.Text.Json.Serialization;

namespace VSOP87
{
    public sealed class VSOPResult_XYZ : VSOPResult
    {
        public override CoordinatesType CoordinatesType => CoordinatesType.Rectangular;

        /// <summary>position x (au)</summary>
        [JsonPropertyName("x")]
        public double x => _variables[0];

        /// <summary>position y (au)</summary>
        [JsonPropertyName("y")]
        public double y => _variables[1];

        /// <summary>position z (au)</summary>
        [JsonPropertyName("z")]
        public double z => _variables[2];

        /// <summary>velocity x (au/day)</summary>
        [JsonPropertyName("dx")]
        public double dx => _variables[3];

        /// <summary>velocity y (au/day)</summary>
        [JsonPropertyName("dy")]
        public double dy => _variables[4];

        /// <summary>velocity z (au/day)</summary>
        [JsonPropertyName("dz")]
        public double dz => _variables[5];

        internal VSOPResult_XYZ(VSOPBody body, VSOPTime time, double[] variables, FrameType frameType, Epoch epoch)
            : base(body, time, variables, frameType, epoch)
        { }

        public override VSOPResult_XYZ ToXYZ() => this;

        public override VSOPResult_LBR ToLBR() =>
            new VSOPResult_LBR(Body, Time, Utility.XYZtoLBR(_variables), FrameType, Epoch);

        public override VSOPResult_ELL ToELL() =>
            new VSOPResult_ELL(Body, Time, Utility.XYZtoELL(Body, _variables), FrameType, Epoch);

        public override VSOPResult_XYZ ChangeEpoch(Epoch target)
        {
            if (Epoch == target) return this;

            // Normalize to ecliptic orientation for precession
            double[] ecliptic = FrameType == FrameType.ICRS
                ? Utility.ICRStoDynamical(_variables)
                : _variables;

            double[] rotated = target == Epoch.OfDate
                ? Utility.J2000toDate(ecliptic, Time)
                : Utility.DatetoJ2000(ecliptic, Time);

            // Denormalize back to original frame
            double[] result = FrameType == FrameType.ICRS
                ? Utility.DynamicaltoICRS(rotated)
                : rotated;

            return new VSOPResult_XYZ(Body, Time, result, FrameType, target);
        }

        public override VSOPResult_XYZ ChangeFrame(FrameType target, Calculator calculator)
        {
            if (FrameType == target) return this;

            // Normalize to J2000 epoch for frame conversion
            double[] j2000 = Epoch == Epoch.OfDate
                ? Utility.DatetoJ2000(_variables, Time)
                : _variables;

            double[] converted = (FrameType, target) switch
            {
                (FrameType.Dynamical, FrameType.ICRS) =>
                    Utility.DynamicaltoICRS(j2000),

                (FrameType.ICRS, FrameType.Dynamical) =>
                    Utility.ICRStoDynamical(j2000),

                (FrameType.Dynamical, FrameType.Barycentric) =>
                    Utility.HelioToBarycentric(j2000, GetSunBary()),

                (FrameType.Barycentric, FrameType.Dynamical) =>
                    Utility.BarycentricToHelio(j2000, GetSunBary()),

                (FrameType.ICRS, FrameType.Barycentric) =>
                    Utility.HelioToBarycentric(Utility.ICRStoDynamical(j2000), GetSunBary()),

                (FrameType.Barycentric, FrameType.ICRS) =>
                    Utility.DynamicaltoICRS(Utility.BarycentricToHelio(j2000, GetSunBary())),

                _ => throw new NotSupportedException($"Unsupported frame conversion: {FrameType} → {target}")
            };

            // Denormalize: restore OfDate epoch
            double[] result = Epoch == Epoch.OfDate
                ? Utility.J2000toDate(converted, Time)
                : converted;

            return new VSOPResult_XYZ(Body, Time, result, target, Epoch);

            double[] GetSunBary() =>
                calculator.GetPlanetPosition(VSOPBody.SUN, VSOPVersion.VSOP87E, Time).Variables.ToArray();
        }
    }
}