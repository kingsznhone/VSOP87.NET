using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSOP87
{
    static class TimeConverter
    {
        /// <summary>
        /// UTC:Coordinated Universal Time
        /// TAI:International Atomic Time
        /// TT :Terrestrial Time
        /// TDB:Barycentric Dynamical Time
        /// </summary>


        #region UTC To TDB
        public static DateTime UTCtoTAI(DateTime UTC)
        {
            return UTC.AddSeconds(37);
        }

        public static DateTime TAItoTT(DateTime TAI)
        {
            return TAI.AddSeconds(32.184);
        }

        public static DateTime TTtoTDB(DateTime TT)
        {
            return TT;
        }

        public static DateTime UTCtoTDB(DateTime UTC)
        {
            return TTtoTDB(TAItoTT(UTCtoTAI(UTC)));
        }
        #endregion

        #region TDB to UTC
        public static DateTime TDBtoTT(DateTime TDB)
        {
            return TDB;
        }

        public static DateTime TTtoTAI(DateTime TT)
        {
            return TT.AddSeconds(-32.184);
        }

        public static DateTime TAItoUTC(DateTime TAI)
        {
            return TAI.AddSeconds(-37);
        }

        public static DateTime TDBtoUTC(DateTime TDB)
        {
            return TDBtoTT(TTtoTAI(TAItoUTC(TDB)));
        }
        #endregion
    }
}
