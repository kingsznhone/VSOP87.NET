using System.Runtime.InteropServices;

namespace VSOP87
{
    [Serializable]
    public enum VSOPBody
    {
        /*
         * 0: Sun
         * 1: Mercury
         * 2: Venus
         * 3: Earth
         * 4: Mars
         * 5: Jupiter
         * 6: Saturn
         * 7: Uranus
         * 8: Neptune
         * 9: Earth-Moon barycenter
         */
        SUN = 0,
        MERCURY = 1,
        VENUS = 2,
        EARTH = 3,
        MARS = 4,
        JUPITER = 5,
        SATURN = 6,
        URANUS = 7,
        NEPTUNE = 8,
        EMB = 9
    }

    [Serializable]
    public enum VSOPVersion
    {
        // 0: VSOP87 (initial solution).
        //    elliptic coordinates
        //    dynamical equinox and ecliptic J2000.
        //  1: VSOP87A.
        //    rectangular coordinates
        //    heliocentric positions and velocities
        //    dynamical equinox and ecliptic J2000.
        //  2: VSOP87A.
        //    spherical coordinates
        //    heliocentric positions and velocities
        //    dynamical equinox and ecliptic J2000.
        //  3: VSOP87C.
        //    rectangular coordinates
        //    heliocentric positions and velocities
        //    dynamical equinox and ecliptic of the date.
        //  4: VSOP87D.
        //    spherical coordinates
        //    heliocentric positions and velocities
        //    dynamical equinox and ecliptic of the date.
        //  5: VSOP87E.
        //    rectangular coordinates
        //    barycentric positions and velocities
        //    dynamical equinox and ecliptic J2000.

        /// <summary>
        /// Elliptic coordinates J2000
        /// </summary>
        VSOP87 = 0,

        /// <summary>
        /// Heliocentric rectangular coordinates J2000
        /// </summary>
        VSOP87A = 1,

        /// <summary>
        /// Heliocentric spherical   coordinates J2000
        /// </summary>
        VSOP87B = 2,

        /// <summary>
        /// Heliocentric rectangular coordinates of date
        /// </summary>
        VSOP87C = 3,

        /// <summary>
        /// Heliocentric spherical   coordinates of date
        /// </summary>
        VSOP87D = 4,

        /// <summary>
        /// Barycentric  rectangular coordinates J2000
        /// </summary>
        VSOP87E = 5,
    }

    [Serializable]
    public struct PlanetTable
    {
        public VSOPVersion version;
        public VSOPBody body;
        public VariableTable[] variables;
    }

    [Serializable]
    public struct PlanetTableF
    {
        public VSOPVersion version;
        public VSOPBody body;
        public VariableTableF[] variables;
    }

    [Serializable]
    public struct VariableTable
    {
        public VSOPVersion version;
        public VSOPBody body;
        public int ic;
        public PowerTable[] PowerTables;
    }

    [Serializable]
    public struct VariableTableF
    {
        public VSOPVersion version;
        public VSOPBody body;
        public int ic;
        public PowerTableF[] PowerTables;
    }

    [Serializable]
    public struct PowerTable
    {
        public VSOPVersion version;
        public VSOPBody body;
        public int ic;
        public int it;
        public Header header;
        public Term[] Terms;

        public double[] Array_A;
        public double[] Array_B;
        public double[] Array_C;
    }

    [Serializable]
    public struct PowerTableF
    {
        public VSOPVersion version;
        public VSOPBody body;
        public int ic;
        public int it;
        public Header header;
        public TermF[] Terms;

        public float[] Array_A;
        public float[] Array_B;
        public float[] Array_C;
    }

    [Serializable]
    public struct Header
    {
        /// <summary>
        /// VSOP87 version
        /// </summary>
        public VSOPVersion Version;

        /// <summary>
        /// number of body
        /// </summary>
        public VSOPBody body;

        /// <summary>
        /// index of coordinate
        /// </summary>
        public int ic;

        /// <summary>
        /// degree alpha of time variable T
        /// </summary>
        public int it;

        /// <summary>
        /// number of terms of series
        /// </summary>
        public int nt;
    }

    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct Term
    {
        /// <summary>
        /// rank of the term in a serie
        /// </summary>
        [FieldOffset(0)]
        public long rank;

        /// <summary>
        /// amplitude A
        /// </summary>
        [FieldOffset(8)]
        public double A;

        /// <summary>
        /// phase     B
        /// </summary>
        [FieldOffset(16)]
        public double B;

        /// <summary>
        /// frequency C
        /// </summary>
        [FieldOffset(24)]
        public double C;
    }

    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct TermF
    {
        /// <summary>
        /// rank of the term in a serie
        /// </summary>
        [FieldOffset(0)]
        public int rank;

        /// <summary>
        /// amplitude A
        /// </summary>
        [FieldOffset(8)]
        public float A;

        /// <summary>
        /// phase     B
        /// </summary>
        [FieldOffset(16)]
        public float B;

        /// <summary>
        /// frequency C
        /// </summary>
        [FieldOffset(24)]
        public float C;
    }
}