namespace VSOP87
{
    public enum CoordinatesReference
    {
        Heliocentric = 0,
        Barycentric = 1
    }

    public enum CoordinatesType
    {
        Elliptic = 0,
        Rectangular = 1,
        Spherical = 2
    }

    public enum TimeFrameReference
    {
        EclipticJ2000 = 0,
        EclipticOfDate = 1
    }

    #region Double

    public abstract class VSOPResult
    {
        public abstract VSOPVersion Version { get; }
        public abstract VSOPBody Body { get; }
        public abstract CoordinatesReference CoordinatesReference { get; }
        public abstract CoordinatesType CoordinatesType { get; }
        public abstract TimeFrameReference TimeFrameReference { get; }
        public abstract VSOPTime Time { get; }
        public abstract double[] Variables { get; set; }
    }

    public class VSOPResult_ELL : VSOPResult
    {
        public override VSOPVersion Version { get; }
        public override VSOPBody Body { get; }

        public override CoordinatesReference CoordinatesReference
        { get { return Utility.GetCoordinatesReference(Version); } }

        public override CoordinatesType CoordinatesType
        { get { return Utility.GetCoordinatesType(Version); } }

        public override TimeFrameReference TimeFrameReference
        { get { return Utility.GetTimeFrameReference(Version); } }

        public override VSOPTime Time { get; }
        public override double[] Variables { get; set; }

        public VSOPResult_ELL(VSOPVersion version, VSOPBody body, VSOPTime time, double[] variables)
        {
            Version = version;
            Body = body;
            Time = time;
            Variables = variables;
        }

        /// <summary>
        /// a = semi-major axis (au)
        /// </summary>
        public double a => Variables[0];

        /// <summary>
        /// lambda = mean longitude (rd)
        /// </summary>
        public double l => Variables[1];

        /// <summary>
        /// k = e*cos(pi) (rd)
        /// </summary>
        public double k => Variables[2];

        /// <summary>
        /// h = e*sin(pi) (rd)
        /// </summary>
        public double h => Variables[3];

        /// <summary>
        /// q = sin(i/2)*cos(omega) (rd)
        /// </summary>
        public double q => Variables[4];

        /// <summary>
        /// p = sin(i/2)*sin(omega) (rd)
        /// </summary>
        public double p => Variables[5];

        public VSOPResult_XYZ ToXYZ()
        {
            return new VSOPResult_XYZ(this.Version, this.Body, this.Time, Utility.ELLtoXYZ(this.Body,Variables));
        }

        public VSOPResult_LBR ToLBR()
        {
            return new VSOPResult_LBR(this.Version, this.Body, this.Time, Utility.ELLtoLBR(this.Body, Variables));
        }
    }

    public class VSOPResult_XYZ : VSOPResult
    {
        public override VSOPVersion Version { get; }
        public override VSOPBody Body { get; }

        public override CoordinatesReference CoordinatesReference
        { get { return Utility.GetCoordinatesReference(Version); } }

        public override CoordinatesType CoordinatesType
        { get { return Utility.GetCoordinatesType(Version); } }

        public override TimeFrameReference TimeFrameReference
        { get { return Utility.GetTimeFrameReference(Version); } }

        public override VSOPTime Time { get; }
        public override double[] Variables { get; set; }

        public VSOPResult_XYZ(VSOPVersion version, VSOPBody body, VSOPTime time, double[] variables)
        {
            Version = version;
            Body = body;
            Time = time;
            Variables = variables;
        }

        /// <summary>
        /// position x (au)
        /// </summary>
        public double x => Variables[0];

        /// <summary>
        /// position y (au)
        /// </summary>
        public double y => Variables[1];

        /// <summary>
        /// position z (au)
        /// </summary>
        public double z => Variables[2];

        /// <summary>
        /// velocity x (au/day)
        /// </summary>
        public double dx => Variables[3];

        /// <summary>
        /// velocity y (au/day)
        /// </summary>
        public double dy => Variables[4];

        /// <summary>
        /// velocity z (au/day)
        /// </summary>
        public double dz => Variables[5];

        public VSOPResult_LBR ToLBR()
        {
            return new VSOPResult_LBR(this.Version, this.Body, this.Time, Utility.XYZtoLBR(Variables));
        }
    }

    public class VSOPResult_LBR : VSOPResult
    {
        public override VSOPVersion Version { get; }
        public override VSOPBody Body { get; }

        public override CoordinatesReference CoordinatesReference
        { get { return Utility.GetCoordinatesReference(Version); } }

        public override CoordinatesType CoordinatesType
        { get { return Utility.GetCoordinatesType(Version); } }

        public override TimeFrameReference TimeFrameReference
        { get { return Utility.GetTimeFrameReference(Version); } }

        public override VSOPTime Time { get; }
        public override double[] Variables { get; set; }

        public VSOPResult_LBR(VSOPVersion version, VSOPBody body, VSOPTime time, double[] variables)
        {
            Version = version;

            Body = body;

            Time = time;

            Variables = variables;
        }

        /// <summary>
        /// longitude (rd)
        /// </summary>
        public double l => Variables[0];

        /// <summary>
        /// latitude (rd)
        /// </summary>
        public double b => Variables[1];

        /// <summary>
        /// radius (au)
        /// </summary>
        public double r => Variables[2];

        /// <summary>
        /// longitude velocity (rd/day)
        /// </summary>
        public double dl => Variables[3];

        /// <summary>
        /// latitude velocity (rd/day)
        /// </summary>
        public double db => Variables[4];

        /// <summary>
        /// radius velocity (au/day)
        /// </summary>
        public double dr => Variables[5];

        public VSOPResult_XYZ ToXYZ()
        {
            return new VSOPResult_XYZ(this.Version, this.Body, this.Time, Utility.LBRtoXYZ(Variables));
        }
    }

    #endregion Double
}