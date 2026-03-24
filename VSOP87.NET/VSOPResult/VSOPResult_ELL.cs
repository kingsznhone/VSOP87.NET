using System.Text.Json.Serialization;

namespace VSOP87
{
    public sealed class VSOPResult_ELL : VSOPResult
    {
        public override CoordinatesType CoordinatesType => CoordinatesType.Elliptic;

        /// <summary>semi-major axis (au)</summary>
        [JsonPropertyName("a")]
        public double a => _variables[0];

        /// <summary>mean longitude (rad)</summary>
        [JsonPropertyName("l")]
        public double l => _variables[1];

        /// <summary>k = e*cos(pi) (dimensionless)</summary>
        [JsonPropertyName("k")]
        public double k => _variables[2];

        /// <summary>h = e*sin(pi) (dimensionless)</summary>
        [JsonPropertyName("h")]
        public double h => _variables[3];

        /// <summary>q = sin(i/2)*cos(omega) (dimensionless)</summary>
        [JsonPropertyName("q")]
        public double q => _variables[4];

        /// <summary>p = sin(i/2)*sin(omega) (dimensionless)</summary>
        [JsonPropertyName("p")]
        public double p => _variables[5];

        public VSOPResult_ELL(VSOPBody body, VSOPTime time, double[] variables, FrameType frameType, Epoch epoch)
            : base(body, time, variables, frameType, epoch)
        { }

        public override VSOPResult_XYZ ToXYZ() =>
            new VSOPResult_XYZ(Body, Time, Utility.ELLtoXYZ(Body, _variables), FrameType, Epoch);

        public override VSOPResult_LBR ToLBR() =>
            new VSOPResult_LBR(Body, Time, Utility.ELLtoLBR(Body, _variables), FrameType, Epoch);

        public override VSOPResult_ELL ToELL() => this;

        public override VSOPResult_ELL ChangeEpoch(Epoch target)
        {
            if (Epoch == target) return this;
            return ToXYZ().ChangeEpoch(target).ToELL();
        }

        public override VSOPResult_ELL ChangeFrame(FrameType target, Calculator calculator)
        {
            if (FrameType == target) return this;
            return ToXYZ().ChangeFrame(target, calculator).ToELL();
        }
    }
}