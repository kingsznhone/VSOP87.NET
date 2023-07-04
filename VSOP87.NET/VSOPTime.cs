﻿namespace VSOP87
{
    public enum TimeFrame
    {
        UTC = 0,
        TAI = 1,
        TT = 2,
        TDB = 3
    }

    public class VSOPTime
    {
        private DateTime _dt { get; set; }

        /// <summary>
        /// UTC:Coordinated Universal Time
        /// </summary>
        public DateTime UTC => _dt;

        /// <summary>
        /// TAI:International Atomic Time
        /// </summary>
        public DateTime TAI => ChangeFrame(_dt, TimeFrame.UTC, TimeFrame.TAI);

        /// <summary>
        /// TT :Terrestrial Time (aka. TDT)
        /// </summary>
        public DateTime TT => ChangeFrame(_dt, TimeFrame.UTC, TimeFrame.TT);

        /// <summary>
        /// TDB:Barycentric Dynamical Time
        /// </summary>
        public DateTime TDB => ChangeFrame(_dt, TimeFrame.UTC, TimeFrame.TDB);

        /// <summary>
        /// Get JD from TDB
        /// </summary>
        public double JulianDate => VSOPTime.ToJulianDate(TDB);

        private List<Func<DateTime, DateTime>> UpGradeFuncs;
        private List<Func<DateTime, DateTime>> DownGradeFuncs;

        public VSOPTime(DateTime UTC)
        {
            if (UTC.Kind != DateTimeKind.Utc)
            {
                this._dt = UTC.ToUniversalTime();
            }
            this._dt = UTC.ToUniversalTime();

            UpGradeFuncs = new List<Func<DateTime, DateTime>>(
                new Func<DateTime, DateTime>[] { UTCtoTAI, TAItoTT, TTtoTDB });

            DownGradeFuncs = new List<Func<DateTime, DateTime>>(
                new Func<DateTime, DateTime>[] { TAItoUTC, TTtoTAI, TDBtoTT });
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="dt">DateTime in UTC Frame</param>
        /// <param name="TargetFrame"></param>
        /// <returns></returns>
        public DateTime ChangeFrame(DateTime dt, TimeFrame SourceFrame, TimeFrame TargetFrame)
        {
            while (SourceFrame != TargetFrame)
            {
                if (TargetFrame > SourceFrame)
                {
                    dt = UpGradeFuncs[(int)SourceFrame](dt);
                    SourceFrame += 1;
                }
                else if (TargetFrame < SourceFrame)
                {
                    dt = DownGradeFuncs[(int)SourceFrame - 1](dt);
                    SourceFrame -= 1;
                }
            }
            return dt;
        }

        #region UTC To TDB

        private static DateTime UTCtoTAI(DateTime UTC)
        {
            return UTC.AddSeconds(37);
        }

        private static DateTime TAItoTT(DateTime TAI)
        {
            return TAI.AddSeconds(32.184);
        }

        private static DateTime TTtoTDB(DateTime TT)
        {
            //Error btw TT&TDB is so small that can be ignored.
            return TT;
        }

        #endregion UTC To TDB



        #region TDB to UTC

        private static DateTime TDBtoTT(DateTime TDB)
        {
            return TDB;
        }

        private static DateTime TTtoTAI(DateTime TT)
        {
            return TT.AddSeconds(-32.184);
        }

        private static DateTime TAItoUTC(DateTime TAI)
        {
            return TAI.AddSeconds(-37);
        }

        #endregion TDB to UTC

        #region JulianDate Convert

        /// <summary>
        ///
        /// </summary>
        /// <param name="dt">Barycentric Dynamical Time</param>
        /// <returns>JulianDate in double</returns>
        public static double ToJulianDate(DateTime dt)
        {
            return dt.ToOADate() + 2415018.5d;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="JD"></param>
        /// <returns>Datetime in TDB Frame</returns>
        public static DateTime FromJulianDate(double JD)
        {  
            return DateTime.FromOADate(JD - 2415018.5).ToUniversalTime();
        }

        #endregion JulianDate Convert

    }
}