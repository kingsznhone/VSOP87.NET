namespace VSOP87
{
    public enum CoordinatesRefrence
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

    public enum ReferenceFrame
    {
        EclipticJ2000 = 0,
        EclipticOfDate = 1
    }

    #region Double

    public abstract class VSOPResult
    {
        public abstract VSOPVersion Version { get; }
        public abstract VSOPBody Body { get; }
        public abstract CoordinatesRefrence CoordinatesRefrence { get; }
        public abstract CoordinatesType CoordinatesType { get; }
        public abstract ReferenceFrame ReferenceFrame { get; }
        public abstract VSOPTime Time { get; }
        public abstract double[] Variables { get; set; }
    }

    public class VSOPResultELL : VSOPResult
    {
        public override VSOPVersion Version { get; }
        public override VSOPBody Body { get; }

        public override CoordinatesRefrence CoordinatesRefrence
        { get { return Utility.GetCoordinatesRefrence(Version); } }

        public override CoordinatesType CoordinatesType
        { get { return Utility.GetCoordinatesType(Version); } }

        public override ReferenceFrame ReferenceFrame
        { get { return Utility.GetFrameRefrence(Version); } }

        public override VSOPTime Time { get; }
        public override double[] Variables { get; set; }

        public VSOPResultELL(VSOPVersion version, VSOPBody body, VSOPTime time, double[] variables)
        {
            Version = version;
            Body = body;
            Time = time;
            Variables = variables;
        }

        /// <summary>
        /// a = semi-major axis (au)
        /// </summary>
        public double a
        { get { return Variables[0]; } }

        /// <summary>
        /// lambda = mean longitude (rd)
        /// </summary>
        public double l
        { get { return Variables[1]; } }

        /// <summary>
        /// k = e*cos(pi) (rd)
        /// </summary>
        public double k
        { get { return Variables[2]; } }

        /// <summary>
        /// h = e*sin(pi) (rd)
        /// </summary>
        public double h
        { get { return Variables[3]; } }

        /// <summary>
        /// q = sin(i/2)*cos(omega) (rd)
        /// </summary>
        public double q
        { get { return Variables[4]; } }

        /// <summary>
        /// p = sin(i/2)*sin(omega) (rd)
        /// </summary>
        public double p
        { get { return Variables[5]; } }
    }

    public class VSOPResultXYZ : VSOPResult
    {
        public override VSOPVersion Version { get; }
        public override VSOPBody Body { get; }

        public override CoordinatesRefrence CoordinatesRefrence
        { get { return Utility.GetCoordinatesRefrence(Version); } }

        public override CoordinatesType CoordinatesType
        { get { return Utility.GetCoordinatesType(Version); } }

        public override ReferenceFrame ReferenceFrame
        { get { return Utility.GetFrameRefrence(Version); } }

        public override VSOPTime Time { get; }
        public override double[] Variables { get; set; }

        public VSOPResultXYZ(VSOPVersion version, VSOPBody body, VSOPTime time, double[] variables)
        {
            Version = version;
            Body = body;
            Time = time;
            Variables = variables;
        }

        /// <summary>
        /// position x (au)
        /// </summary>
        public double x
        { get { return Variables[0]; } }

        /// <summary>
        /// position y (au)
        /// </summary>
        public double y
        { get { return Variables[1]; } }

        /// <summary>
        /// position z (au)
        /// </summary>
        public double z
        { get { return Variables[2]; } }

        /// <summary>
        /// velocity x (au/day)
        /// </summary>
        public double dx
        { get { return Variables[3]; } }

        /// <summary>
        /// velocity y (au/day)
        /// </summary>
        public double dy
        { get { return Variables[4]; } }

        /// <summary>
        /// velocity z (au/day)
        /// </summary>
        public double dz
        { get { return Variables[5]; } }
    }

    public class VSOPResultLBR : VSOPResult
    {
        public override VSOPVersion Version { get; }
        public override VSOPBody Body { get; }

        public override CoordinatesRefrence CoordinatesRefrence
        { get { return Utility.GetCoordinatesRefrence(Version); } }

        public override CoordinatesType CoordinatesType
        { get { return Utility.GetCoordinatesType(Version); } }

        public override ReferenceFrame ReferenceFrame
        { get { return Utility.GetFrameRefrence(Version); } }

        public override VSOPTime Time { get; }
        public override double[] Variables { get; set; }

        public VSOPResultLBR(VSOPVersion version, VSOPBody body, VSOPTime time, double[] variables)
        {
            Version = version;

            Body = body;

            Time = time;

            Variables = variables;
        }

        /// <summary>
        /// longitude (rd)
        /// </summary>
        public double L
        { get { return Variables[0]; } }

        /// <summary>
        /// latitude (rd)
        /// </summary>
        public double B
        { get { return Variables[1]; } }

        /// <summary>
        /// radius (au)
        /// </summary>
        public double R
        { get { return Variables[2]; } }

        /// <summary>
        /// longitude velocity (rd/day)
        /// </summary>
        public double dL
        { get { return Variables[3]; } }

        /// <summary>
        /// latitude velocity (rd/day)
        /// </summary>
        public double dB
        { get { return Variables[4]; } }

        /// <summary>
        /// radius velocity (au/day)
        /// </summary>
        public double dR
        { get { return Variables[5]; } }
    }

    #endregion Double

    #region Float

    public abstract class VSOPResultF
    {
        public abstract VSOPVersion Version { get; }
        public abstract VSOPBody Body { get; }
        public abstract CoordinatesRefrence CoordinatesRefrence { get; }
        public abstract CoordinatesType CoordinatesType { get; }
        public abstract ReferenceFrame ReferenceFrame { get; }
        public abstract VSOPTime Time { get; }
        public abstract float[] Variables { get; set; }
    }

    public class VSOPResultFELL : VSOPResultF
    {
        public override VSOPVersion Version { get; }
        public override VSOPBody Body { get; }

        public override CoordinatesRefrence CoordinatesRefrence
        { get { return Utility.GetCoordinatesRefrence(Version); } }

        public override CoordinatesType CoordinatesType
        { get { return Utility.GetCoordinatesType(Version); } }

        public override ReferenceFrame ReferenceFrame
        { get { return Utility.GetFrameRefrence(Version); } }

        public override VSOPTime Time { get; }
        public override float[] Variables { get; set; }

        public VSOPResultFELL(VSOPVersion version, VSOPBody body, VSOPTime time, float[] variables)
        {
            Version = version;
            Body = body;
            Time = time;
            Variables = variables;
        }

        /// <summary>
        /// a = semi-major axis (au)
        /// </summary>
        public float a
        { get { return Variables[0]; } }

        /// <summary>
        /// lambda = mean longitude (rd)
        /// </summary>
        public float l
        { get { return Variables[1]; } }

        /// <summary>
        /// k = e*cos(pi) (rd)
        /// </summary>
        public float k
        { get { return Variables[2]; } }

        /// <summary>
        /// h = e*sin(pi) (rd)
        /// </summary>
        public float h
        { get { return Variables[3]; } }

        /// <summary>
        /// q = sin(i/2)*cos(omega) (rd)
        /// </summary>
        public float q
        { get { return Variables[4]; } }

        /// <summary>
        /// p = sin(i/2)*sin(omega) (rd)
        /// </summary>
        public float p
        { get { return Variables[5]; } }
    }

    public class VSOPResultFXYZ : VSOPResultF
    {
        public override VSOPVersion Version { get; }
        public override VSOPBody Body { get; }

        public override CoordinatesRefrence CoordinatesRefrence
        { get { return Utility.GetCoordinatesRefrence(Version); } }

        public override CoordinatesType CoordinatesType
        { get { return Utility.GetCoordinatesType(Version); } }

        public override ReferenceFrame ReferenceFrame
        { get { return Utility.GetFrameRefrence(Version); } }

        public override VSOPTime Time { get; }
        public override float[] Variables { get; set; }

        public VSOPResultFXYZ(VSOPVersion version, VSOPBody body, VSOPTime time, float[] variables)
        {
            Version = version;
            Body = body;
            Time = time;
            Variables = variables;
        }

        /// <summary>
        /// position x (au)
        /// </summary>
        public float x
        { get { return Variables[0]; } }

        /// <summary>
        /// position y (au)
        /// </summary>
        public float y
        { get { return Variables[1]; } }

        /// <summary>
        /// position z (au)
        /// </summary>
        public float z
        { get { return Variables[2]; } }

        /// <summary>
        /// velocity x (au/day)
        /// </summary>
        public float dx
        { get { return Variables[3]; } }

        /// <summary>
        /// velocity y (au/day)
        /// </summary>
        public float dy
        { get { return Variables[4]; } }

        /// <summary>
        /// velocity z (au/day)
        /// </summary>
        public float dz
        { get { return Variables[5]; } }
    }

    public class VSOPResultFLBR : VSOPResultF
    {
        public override VSOPVersion Version { get; }
        public override VSOPBody Body { get; }

        public override CoordinatesRefrence CoordinatesRefrence
        { get { return Utility.GetCoordinatesRefrence(Version); } }

        public override CoordinatesType CoordinatesType
        { get { return Utility.GetCoordinatesType(Version); } }

        public override ReferenceFrame ReferenceFrame
        { get { return Utility.GetFrameRefrence(Version); } }

        public override VSOPTime Time { get; }
        public override float[] Variables { get; set; }

        public VSOPResultFLBR(VSOPVersion version, VSOPBody body, VSOPTime time, float[] variables)
        {
            Version = version;

            Body = body;

            Time = time;

            Variables = variables;
        }

        /// <summary>
        /// longitude (rd)
        /// </summary>
        public float L
        { get { return Variables[0]; } }

        /// <summary>
        /// latitude (rd)
        /// </summary>
        public float B
        { get { return Variables[1]; } }

        /// <summary>
        /// radius (au)
        /// </summary>
        public float R
        { get { return Variables[2]; } }

        /// <summary>
        /// longitude velocity (rd/day)
        /// </summary>
        public float dL
        { get { return Variables[3]; } }

        /// <summary>
        /// latitude velocity (rd/day)
        /// </summary>
        public float dB
        { get { return Variables[4]; } }

        /// <summary>
        /// radius velocity (au/day)
        /// </summary>
        public float dR
        { get { return Variables[5]; } }
    }

    #endregion Float
}