using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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

        /* 0: VSOP87 (initial solution).
        *    elliptic coordinates
        *    dynamical equinox and ecliptic J2000.
        *  1: VSOP87A.
        *    rectangular coordinates
        *    heliocentric positions and velocities
        *    dynamical equinox and ecliptic J2000.
        *  2: VSOP87A.
        *    spherical coordinates
        *    heliocentric positions and velocities
        *    dynamical equinox and ecliptic J2000.
        *  3: VSOP87C.
        *    rectangular coordinates
        *    heliocentric positions and velocities
        *    dynamical equinox and ecliptic of the date.
        *  4: VSOP87D.
        *    spherical coordinates
        *    heliocentric positions and velocities
        *    dynamical equinox and ecliptic of the date.
        *  5: VSOP87E.
        *    rectangular coordinates
        *    barycentric positions and velocities
        *    dynamical equinox and ecliptic J2000.
        */

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
        /// VSOP87C : Heliocentric rectangular coordinates of date
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
        public VariableTable[] variables;
        public VSOPVersion iver;
        public VSOPBody ibody;
    }

    [Serializable]
    public struct VariableTable
    {
        public PowerTable[] PowerTables;
    }

    [Serializable]
    public struct PowerTable
    {
        public Header header;
        public Term[] Terms;
    }

    [Serializable]
    public struct Header
    {
        public int iv; //VSOP87 version
        public string body; //name of body
        public int ib; //number of body
        public int ic; //index of coordinate
        public int it; //degree alpha of time variable T 
        public int nt; //number of terms of series
    }

    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct Term
    {
        [FieldOffset(0)]
        public long rank; //rank of the term in a serie
        [FieldOffset(8)]
        public double A; //amplitude A
        [FieldOffset(16)]
        public double B; //phase     B   
        [FieldOffset(24)]
        public double C; //frequency C
    }

}
