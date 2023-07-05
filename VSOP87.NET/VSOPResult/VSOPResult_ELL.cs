namespace VSOP87
{
    public class VSOPResult_ELL : VSOPResult
    {
        public override VSOPVersion Version { get; }
        public override VSOPBody Body { get; }
        public override CoordinatesReference CoordinatesReference => CoordinatesReference.EclipticHeliocentric;
        public override CoordinatesType CoordinatesType => CoordinatesType.Elliptic;

        public override ReferenceFrame ReferenceFrame
        {
            get { return ReferenceFrame.DynamicalJ2000; }
            set { throw new NotSupportedException("Can't convert reference frame on elliptic coordinate."); }
        }

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
            return new VSOPResult_XYZ(this);
        }

        public VSOPResult_LBR ToLBR()
        {
            return new VSOPResult_LBR(this);
        }
    }
}