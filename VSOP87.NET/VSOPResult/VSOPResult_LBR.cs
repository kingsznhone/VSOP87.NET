namespace VSOP87
{
    public class VSOPResult_LBR : VSOPResult
    {
        public override VSOPVersion Version { get; }
        public override VSOPBody Body { get; }

        private CoordinatesReference _coordinatesReference;

        public override CoordinatesReference CoordinatesReference
        {
            get
            {
                if (_coordinatesReference == CoordinatesReference.EclipticBarycentric)
                    return this._coordinatesReference;
                else
                {
                    if (_referenceFrame == ReferenceFrame.DynamicalJ2000)
                    {
                        return CoordinatesReference.EclipticHeliocentric;
                    }
                    else
                    {
                        return CoordinatesReference.EquatorialHeliocentric;
                    }
                }
            }
        }

        public override CoordinatesType CoordinatesType => CoordinatesType.Spherical;

        private ReferenceFrame _referenceFrame;

        public override ReferenceFrame ReferenceFrame
        {
            get { return _referenceFrame; }
            set
            {
                if (_referenceFrame == ReferenceFrame.DynamicalDate)
                {
                    throw new NotSupportedException("'Dynamical frame of the date' is not supported.");
                }
                if (CoordinatesReference == CoordinatesReference.EclipticBarycentric)
                {
                    throw new NotSupportedException("'Barycentric Coordinates' is not supported.");
                }
                if (_referenceFrame == ReferenceFrame.DynamicalJ2000 && value == ReferenceFrame.ICRSJ2000)
                {
                    Variables = Utility.XYZtoLBR(Utility.DynamicaltoICRS(Utility.LBRtoXYZ(Variables)));
                    _referenceFrame = value;
                    _coordinatesReference = CoordinatesReference.EquatorialHeliocentric;
                }
                else if (_referenceFrame == ReferenceFrame.ICRSJ2000 && value == ReferenceFrame.DynamicalJ2000)
                {
                    Variables = Utility.XYZtoLBR(Utility.ICRStoDynamical(Utility.LBRtoXYZ(Variables)));
                    _referenceFrame = value;
                    _coordinatesReference = CoordinatesReference.EclipticHeliocentric;
                }
            }
        }

        public override VSOPTime Time { get; }
        public override double[] Variables { get; set; }

        public VSOPResult_LBR(VSOPVersion version, VSOPBody body, VSOPTime time, double[] variables)
        {
            Version = version;
            if (version == VSOPVersion.VSOP87B)
            {
                _referenceFrame = ReferenceFrame.DynamicalJ2000;
            }
            else
            {
                _referenceFrame = ReferenceFrame.DynamicalDate;
            }
            Body = body;

            Time = time;

            Variables = variables;
        }

        public VSOPResult_LBR(VSOPResult_XYZ result)
        {
            Version = result.Version;
            _coordinatesReference = result.CoordinatesReference;
            _referenceFrame = result.ReferenceFrame;
            Body = result.Body;
            Time = result.Time;
            Variables = Utility.XYZtoLBR(result.Variables);
        }

        public VSOPResult_LBR(VSOPResult_ELL result)
        {
            Version = result.Version;
            _coordinatesReference = result.CoordinatesReference;
            _referenceFrame = result.ReferenceFrame;
            Body = result.Body;
            Time = result.Time;
            Variables = Utility.ELLtoLBR(result.Body, result.Variables);
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

        //public VSOPResult_XYZ ToXYZ()
        //{
        //    return new VSOPResult_XYZ(this.Version, this.Body, this.Time, Utility.LBRtoXYZ(Variables));
        //}
        public VSOPResult_XYZ ToXYZ()
        {
            return new VSOPResult_XYZ(this);
        }
    }
}