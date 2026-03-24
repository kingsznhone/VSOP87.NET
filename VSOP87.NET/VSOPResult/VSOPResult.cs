using System.Text.Json.Serialization;

namespace VSOP87
{
    [JsonConverter(typeof(JsonStringEnumConverter<CoordinatesType>))]
    public enum CoordinatesType
    {
        Elliptic = 0,
        Rectangular = 1,
        Spherical = 2
    }

    [JsonConverter(typeof(JsonStringEnumConverter<FrameType>))]
    public enum FrameType
    {
        /// <summary>
        /// Dynamical equinox and ecliptic, heliocentric origin.
        /// </summary>
        Dynamical = 0,

        /// <summary>
        /// ICRS equatorial frame, heliocentric origin.
        /// </summary>
        ICRS = 1,

        /// <summary>
        /// Dynamical equinox and ecliptic, Solar System Barycenter (SSB) origin.
        /// </summary>
        Barycentric = 2,
    }

    [JsonConverter(typeof(JsonStringEnumConverter<Epoch>))]
    public enum Epoch
    {
        /// <summary>
        /// Fixed J2000.0 ecliptic and equinox.
        /// </summary>
        J2000 = 0,

        /// <summary>
        /// Dynamical ecliptic and equinox of the observation date (includes precession).
        /// </summary>
        OfDate = 1,
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(VSOPResult_ELL), "ELL")]
    [JsonDerivedType(typeof(VSOPResult_XYZ), "XYZ")]
    [JsonDerivedType(typeof(VSOPResult_LBR), "LBR")]
    public abstract class VSOPResult
    {
        public VSOPBody Body { get; }

        public abstract CoordinatesType CoordinatesType { get; }

        public FrameType FrameType { get; }

        public Epoch Epoch { get; }

        public VSOPTime Time { get; }

        protected readonly double[] _variables;

        /// <summary>
        /// Raw 6-element coordinate array (read-only view).
        /// </summary>
        [JsonIgnore]
        public ReadOnlySpan<double> Variables => _variables;

        protected VSOPResult(VSOPBody body, VSOPTime time, double[] variables,
            FrameType frameType, Epoch epoch)
        {
            Body = body;
            Time = time;
            _variables = variables;
            FrameType = frameType;
            Epoch = epoch;
        }

        #region Coordinate type conversions

        /// <summary>
        /// Convert to rectangular (XYZ) coordinates, preserving frame and epoch.
        /// </summary>
        public abstract VSOPResult_XYZ ToXYZ();

        /// <summary>
        /// Convert to spherical (LBR) coordinates, preserving frame and epoch.
        /// </summary>
        public abstract VSOPResult_LBR ToLBR();

        /// <summary>
        /// Convert to elliptic (ELL) elements, preserving frame and epoch.
        /// </summary>
        public abstract VSOPResult_ELL ToELL();

        #endregion Coordinate type conversions

        #region Frame and epoch conversions

        /// <summary>
        /// Returns a new result with coordinates rotated to the target epoch.
        /// Each derived class implements its own relay conversion and returns its own coordinate type.
        /// </summary>
        public abstract VSOPResult ChangeEpoch(Epoch target);

        /// <summary>
        /// Returns a new result expressed in the target frame type.
        /// Each derived class implements its own relay conversion and returns its own coordinate type.
        /// </summary>
        /// <param name="target">Target frame type</param>
        /// <param name="calculator">Calculator instance used to obtain the Sun's barycentric position from VSOP87E</param>
        public abstract VSOPResult ChangeFrame(FrameType target, Calculator calculator);

        #endregion Frame and epoch conversions
    }
}