namespace VSOP87
{
    public enum CoordinatesReference
    {
        EclipticHeliocentric = 0,
        EclipticBarycentric = 1,
        EquatorialHeliocentric = 2
    }

    public enum CoordinatesType
    {
        Elliptic = 0,
        Rectangular = 1,
        Spherical = 2
    }

    public enum ReferenceFrame
    {
        /// <summary>
        /// dynamical equinox and ecliptic J2000 (Ecliptic)
        /// </summary>
        DynamicalJ2000 = 0,

        /// <summary>
        /// dynamical equinox and ecliptic of date (Ecliptic)
        /// </summary>
        DynamicalDate = 1,

        /// <summary>
        /// ICRS Frame J2000 (Equatorial)
        /// </summary>
        ICRSJ2000 = 2
    }

    public abstract class VSOPResult
    {
        /// <summary>
        /// Version of this result generate from
        /// </summary>
        public abstract VSOPVersion Version { get; }

        /// <summary>
        /// Planet of this result
        /// </summary>
        public abstract VSOPBody Body { get; }

        /// <summary>
        /// Coordinates Reference
        /// </summary>
        public abstract CoordinatesReference CoordinatesReference { get; }

        /// <summary>
        /// Coordinates Type
        /// </summary>
        public abstract CoordinatesType CoordinatesType { get; }

        /// <summary>
        /// Inertial Frame
        /// </summary>
        public abstract ReferenceFrame ReferenceFrame { get; set; }

        /// <summary>
        /// VSOPTime class
        /// </summary>
        public abstract VSOPTime Time { get; }

        /// <summary>
        /// Raw data store
        /// </summary>
        public abstract double[] Variables { get; set; }
    }
}