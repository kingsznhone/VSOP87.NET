﻿using MessagePack;
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
        EMB = 9,
        PLUTO = 10 // Not exsist in VSOP87
    }

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

    [MessagePackObject]
    [Serializable]
    public struct PlanetTable
    {
        [Key(0)]
        public VSOPVersion version;

        [Key(1)]
        public VSOPBody body;

        [Key(2)]
        public VariableTable[] variables;
    }

    [MessagePackObject]
    [Serializable]
    public struct VariableTable
    {
        [Key(0)]
        public VSOPVersion version;

        [Key(1)]
        public VSOPBody body;

        [Key(2)]
        public int ic;

        [Key(3)]
        public PowerTable[] PowerTables;
    }

    [MessagePackObject]
    [Serializable]
    public struct PowerTable
    {
        [Key(0)]
        public VSOPVersion version;

        [Key(1)]
        public VSOPBody body;

        [Key(2)]
        public int ic;

        [Key(3)]
        public int it;

        [Key(4)]
        public Header header;

        [Key(5)]
        public Term[] Terms;
    }

    [MessagePackObject]
    [Serializable]
    public struct Header
    {
        [Key(0)]
        public VSOPVersion Version;

        [Key(1)]
        public VSOPBody body;

        [Key(2)]
        public int ic;

        [Key(3)]
        public int it;

        [Key(4)]
        public int nt;
    }

    [MessagePackObject]
    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct Term
    {
        /// <summary>
        /// rank of the term in a serie
        /// </summary>
        [Key(0)]
        [FieldOffset(0)]
        public long rank;

        /// <summary>
        /// amplitude A
        /// </summary>
        [Key(1)]
        [FieldOffset(8)]
        public double A;

        /// <summary>
        /// phase     B
        /// </summary>
        [Key(2)]
        [FieldOffset(16)]
        public double B;

        /// <summary>
        /// frequency C
        /// </summary>
        [Key(3)]
        [FieldOffset(24)]
        public double C;
    }
}