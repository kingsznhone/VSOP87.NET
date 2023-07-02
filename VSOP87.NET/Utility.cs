namespace VSOP87
{
    public static class Utility
    {
        public static List<VSOPBody> AvailableBody(VSOPVersion ver) => ver switch
        {
            VSOPVersion.VSOP87 => new List<VSOPBody>(new VSOPBody[] {
                        VSOPBody.MERCURY,
                        VSOPBody.VENUS,
                        VSOPBody.MARS,
                        VSOPBody.JUPITER,
                        VSOPBody.SATURN,
                        VSOPBody.URANUS,
                        VSOPBody.NEPTUNE,
                        VSOPBody.EMB }),

            VSOPVersion.VSOP87A => new List<VSOPBody>(new VSOPBody[] {
                        VSOPBody.MERCURY,
                        VSOPBody.VENUS,
                        VSOPBody.EARTH,
                        VSOPBody.MARS,
                        VSOPBody.JUPITER,
                        VSOPBody.SATURN,
                        VSOPBody.URANUS,
                        VSOPBody.NEPTUNE,
                        VSOPBody.EMB }),

            VSOPVersion.VSOP87B => new List<VSOPBody>(new VSOPBody[] {
                        VSOPBody.MERCURY,
                        VSOPBody.VENUS,
                        VSOPBody.EARTH,
                        VSOPBody.MARS,
                        VSOPBody.JUPITER,
                        VSOPBody.SATURN,
                        VSOPBody.URANUS,
                        VSOPBody.NEPTUNE,
                        }),

            VSOPVersion.VSOP87C => new List<VSOPBody>(new VSOPBody[] {
                        VSOPBody.MERCURY,
                        VSOPBody.VENUS,
                        VSOPBody.EARTH,
                        VSOPBody.MARS,
                        VSOPBody.JUPITER,
                        VSOPBody.SATURN,
                        VSOPBody.URANUS,
                        VSOPBody.NEPTUNE,
                        }),

            VSOPVersion.VSOP87D => new List<VSOPBody>(new VSOPBody[] {
                        VSOPBody.MERCURY,
                        VSOPBody.VENUS,
                        VSOPBody.EARTH,
                        VSOPBody.MARS,
                        VSOPBody.JUPITER,
                        VSOPBody.SATURN,
                        VSOPBody.URANUS,
                        VSOPBody.NEPTUNE,
                        }),

            VSOPVersion.VSOP87E => new List<VSOPBody>(new VSOPBody[] {
                        VSOPBody.SUN,
                        VSOPBody.MERCURY,
                        VSOPBody.VENUS,
                        VSOPBody.EARTH,
                        VSOPBody.MARS,
                        VSOPBody.JUPITER,
                        VSOPBody.SATURN,
                        VSOPBody.URANUS,
                        VSOPBody.NEPTUNE,
                        }),

            _ => new List<VSOPBody>()
        };

        public static bool CheckAvailability(VSOPVersion ver, VSOPBody body)
        {
            return AvailableBody(ver).Exists(x => x == body);
        }

        public static CoordinatesType GetCoordinatesType(VSOPVersion ver) => ver switch
        {
            VSOPVersion.VSOP87 => CoordinatesType.Elliptic,
            VSOPVersion.VSOP87A or VSOPVersion.VSOP87C or VSOPVersion.VSOP87E => CoordinatesType.Rectangular,
            VSOPVersion.VSOP87B or VSOPVersion.VSOP87D => CoordinatesType.Spherical,
            _ => throw new ArgumentException()
        };

        public static CoordinatesReference GetCoordinatesReference(VSOPVersion ver) => ver switch
        {
            VSOPVersion.VSOP87E => CoordinatesReference.Barycentric,
            _ => CoordinatesReference.Heliocentric
        };

        public static TimeFrameReference GetTimeFrameReference(VSOPVersion ver) => ver switch
        {
            VSOPVersion.VSOP87C or VSOPVersion.VSOP87D => TimeFrameReference.EclipticOfDate,
            _ => TimeFrameReference.EclipticJ2000
        };
    }
}