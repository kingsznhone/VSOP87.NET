namespace VSOP87
{
    public class VSOPResult_XYZ : VSOPResult
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

        public override CoordinatesType CoordinatesType => CoordinatesType.Rectangular;

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
                if (_referenceFrame == ReferenceFrame.DynamicalJ2000)
                {
                    Variables = Utility.DynamicaltoICRS(Variables);
                    _referenceFrame = value;
                    _coordinatesReference = CoordinatesReference.EquatorialHeliocentric;
                }
                else if (_referenceFrame == ReferenceFrame.ICRSJ2000)
                {
                    Variables = Utility.ICRStoDynamical(Variables);
                    _referenceFrame = ReferenceFrame.DynamicalJ2000;
                    _coordinatesReference = CoordinatesReference.EclipticHeliocentric;
                }
            }
        }

        public override VSOPTime Time { get; }
        public override double[] Variables { get; set; }

        /// <summary>
        /// From calculator result
        /// </summary>
        /// <param name="version"></param>
        /// <param name="body"></param>
        /// <param name="time"></param>
        /// <param name="variables"></param>
        public VSOPResult_XYZ(VSOPVersion version, VSOPBody body, VSOPTime time, double[] variables)
        {
            Version = version;

            if (version == VSOPVersion.VSOP87E)
            {
                _coordinatesReference = CoordinatesReference.EclipticBarycentric;
            }
            else
            {
                _coordinatesReference = CoordinatesReference.EclipticHeliocentric;
            }

            if (version == VSOPVersion.VSOP87C)
            {
                _referenceFrame = ReferenceFrame.DynamicalDate;
            }
            else
            {
                _referenceFrame = ReferenceFrame.DynamicalJ2000;
            }

            Body = body;
            Time = time;
            Variables = variables;
        }

        public VSOPResult_XYZ(VSOPResult_LBR result)
        {
            Version = result.Version;
            _coordinatesReference = result.CoordinatesReference;
            _referenceFrame = result.ReferenceFrame;
            Body = result.Body;
            Time = result.Time;
            Variables = Utility.LBRtoXYZ(result.Variables);
        }

        public VSOPResult_XYZ(VSOPResult_ELL result)
        {
            Version = result.Version;
            _coordinatesReference = result.CoordinatesReference;
            _referenceFrame = result.ReferenceFrame;
            Body = result.Body;
            Time = result.Time;
            Variables = Utility.ELLtoXYZ(result.Body, result.Variables);
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
            return new VSOPResult_LBR(this);
        }
    }
}