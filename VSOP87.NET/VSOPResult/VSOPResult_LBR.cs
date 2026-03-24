using System.Text.Json.Serialization;

namespace VSOP87
{
    public sealed class VSOPResult_LBR : VSOPResult
    {
        public override CoordinatesType CoordinatesType => CoordinatesType.Spherical;

        /// <summary>longitude (rad)</summary>
        [JsonPropertyName("l")]
        public double l => _variables[0];

        /// <summary>latitude (rad)</summary>
        [JsonPropertyName("b")]
        public double b => _variables[1];

        /// <summary>radius (au)</summary>
        [JsonPropertyName("r")]
        public double r => _variables[2];

        /// <summary>longitude velocity (rad/day)</summary>
        [JsonPropertyName("dl")]
        public double dl => _variables[3];

        /// <summary>latitude velocity (rad/day)</summary>
        [JsonPropertyName("db")]
        public double db => _variables[4];

        /// <summary>radius velocity (au/day)</summary>
        [JsonPropertyName("dr")]
        public double dr => _variables[5];

        public VSOPResult_LBR(VSOPBody body, VSOPTime time, double[] variables, FrameType frameType, Epoch epoch)
            : base(body, time, variables, frameType, epoch)
        { }

        public override VSOPResult_XYZ ToXYZ() =>
            new VSOPResult_XYZ(Body, Time, Utility.LBRtoXYZ(_variables), FrameType, Epoch);

        public override VSOPResult_LBR ToLBR() => this;

        public override VSOPResult_ELL ToELL() =>
            new VSOPResult_ELL(Body, Time, Utility.LBRtoELL(Body, _variables), FrameType, Epoch);

        public override VSOPResult_LBR ChangeEpoch(Epoch target)
        {
            if (Epoch == target) return this;
            return ToXYZ().ChangeEpoch(target).ToLBR();
        }

        public override VSOPResult_LBR ChangeFrame(FrameType target, Calculator calculator)
        {
            if (FrameType == target) return this;
            return ToXYZ().ChangeFrame(target, calculator).ToLBR();
        }
    }
}