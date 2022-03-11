namespace VSOP87
{
    public static class Utility
    {
        public static List<VSOPBody> AvailableBody(VSOPVersion ver)
        {
            switch (ver)
            {
                case VSOPVersion.VSOP87:
                    return new List<VSOPBody>(new VSOPBody[] {
                        VSOPBody.MERCURY,
                        VSOPBody.VENUS,
                        VSOPBody.MARS,
                        VSOPBody.JUPITER,
                        VSOPBody.SATURN,
                        VSOPBody.URANUS,
                        VSOPBody.NEPTUNE,
                        VSOPBody.EMB });

                case VSOPVersion.VSOP87A:
                    return new List<VSOPBody>(new VSOPBody[] {
                        VSOPBody.MERCURY,
                        VSOPBody.VENUS,
                        VSOPBody.EARTH,
                        VSOPBody.MARS,
                        VSOPBody.JUPITER,
                        VSOPBody.SATURN,
                        VSOPBody.URANUS,
                        VSOPBody.NEPTUNE,
                        VSOPBody.EMB });

                case VSOPVersion.VSOP87B:
                    return new List<VSOPBody>(new VSOPBody[] {
                        VSOPBody.MERCURY,
                        VSOPBody.VENUS,
                        VSOPBody.EARTH,
                        VSOPBody.MARS,
                        VSOPBody.JUPITER,
                        VSOPBody.SATURN,
                        VSOPBody.URANUS,
                        VSOPBody.NEPTUNE,
                        });

                case VSOPVersion.VSOP87C:
                    return new List<VSOPBody>(new VSOPBody[] {
                        VSOPBody.MERCURY,
                        VSOPBody.VENUS,
                        VSOPBody.EARTH,
                        VSOPBody.MARS,
                        VSOPBody.JUPITER,
                        VSOPBody.SATURN,
                        VSOPBody.URANUS,
                        VSOPBody.NEPTUNE,
                        });

                case VSOPVersion.VSOP87D:
                    return new List<VSOPBody>(new VSOPBody[] {
                        VSOPBody.MERCURY,
                        VSOPBody.VENUS,
                        VSOPBody.EARTH,
                        VSOPBody.MARS,
                        VSOPBody.JUPITER,
                        VSOPBody.SATURN,
                        VSOPBody.URANUS,
                        VSOPBody.NEPTUNE,
                        });

                case VSOPVersion.VSOP87E:
                    return new List<VSOPBody>(new VSOPBody[] {
                        VSOPBody.SUN,
                        VSOPBody.MERCURY,
                        VSOPBody.VENUS,
                        VSOPBody.EARTH,
                        VSOPBody.MARS,
                        VSOPBody.JUPITER,
                        VSOPBody.SATURN,
                        VSOPBody.URANUS,
                        VSOPBody.NEPTUNE,
                        });

                default: return new List<VSOPBody>();
            }
        }

        public static bool CheckAvailability(VSOPVersion ver, VSOPBody body)
        {
            return AvailableBody(ver).Exists(x => x == body);
        }

        public static CoordinatesType GetCoordinatesType(VSOPVersion ver)
        {
            switch (ver)
            {
                case VSOPVersion.VSOP87:
                    return CoordinatesType.Elliptic;
                case VSOPVersion.VSOP87A or VSOPVersion.VSOP87C or VSOPVersion.VSOP87E:
                    return CoordinatesType.Rectangular;
                case VSOPVersion.VSOP87B or VSOPVersion.VSOP87D:
                    return CoordinatesType.Spherical;
                default: throw new ArgumentException();
            }
        }

        public static CoordinatesRefrence GetCoordinatesRefrence(VSOPVersion ver)
        {
            switch (ver)
            {
                case VSOPVersion.VSOP87E:
                    return CoordinatesRefrence.Barycentric;

                default: return CoordinatesRefrence.Heliocentric;
            }
        }

        public static ReferenceFrame GetFrameRefrence(VSOPVersion ver)
        {
            switch (ver)
            {
                case VSOPVersion.VSOP87C or VSOPVersion.VSOP87D:
                    return ReferenceFrame.EclipticOfDate;
                default: return ReferenceFrame.EclipticJ2000;
            }
        }


    }

}
