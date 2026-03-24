using System.Numerics;
using System.Runtime.Intrinsics;

namespace VSOP87
{
    public static class Utility
    {
        private static readonly double[] gmp = {
                        2.9591220836841438269e-04d,//sun (for body enum alignment)
                        4.9125474514508118699e-11d,//Mer
                        7.2434524861627027000e-10d,//Venus
                        8.9970116036316091182e-10d,//earth (not exsist in vsop2013)
                        9.5495351057792580598e-11d,//mars
                        2.8253458420837780000e-07d,//jupiter
                        8.4597151856806587398e-08d,//saturn
                        1.2920249167819693900e-08d,//uranus
                        1.5243589007842762800e-08d,//neptune
                        8.9970116036316091182e-10d,//EMB
        };

        /// <summary>
        /// List all available planet in selected version
        /// </summary>
        /// <param name="ver">version enum</param>
        /// <returns>List of planets</returns>
        public static List<VSOPBody> ListAvailableBody(VSOPVersion ver) => ver switch
        {
            VSOPVersion.VSOP87 =>
            [
                VSOPBody.MERCURY,
                VSOPBody.VENUS,
                VSOPBody.MARS,
                VSOPBody.JUPITER,
                VSOPBody.SATURN,
                VSOPBody.URANUS,
                VSOPBody.NEPTUNE,
                VSOPBody.EMB
            ],

            VSOPVersion.VSOP87A =>
            [
                VSOPBody.MERCURY,
                VSOPBody.VENUS,
                VSOPBody.EARTH,
                VSOPBody.MARS,
                VSOPBody.JUPITER,
                VSOPBody.SATURN,
                VSOPBody.URANUS,
                VSOPBody.NEPTUNE,
                VSOPBody.EMB
            ],

            VSOPVersion.VSOP87B =>
            [
                VSOPBody.MERCURY,
                VSOPBody.VENUS,
                VSOPBody.EARTH,
                VSOPBody.MARS,
                VSOPBody.JUPITER,
                VSOPBody.SATURN,
                VSOPBody.URANUS,
                VSOPBody.NEPTUNE,
            ],

            VSOPVersion.VSOP87C =>
            [
                VSOPBody.MERCURY,
                VSOPBody.VENUS,
                VSOPBody.EARTH,
                VSOPBody.MARS,
                VSOPBody.JUPITER,
                VSOPBody.SATURN,
                VSOPBody.URANUS,
                VSOPBody.NEPTUNE,
            ],

            VSOPVersion.VSOP87D =>
            [
                VSOPBody.MERCURY,
                VSOPBody.VENUS,
                VSOPBody.EARTH,
                VSOPBody.MARS,
                VSOPBody.JUPITER,
                VSOPBody.SATURN,
                VSOPBody.URANUS,
                VSOPBody.NEPTUNE,
            ],

            VSOPVersion.VSOP87E =>
            [
                VSOPBody.SUN,
                VSOPBody.MERCURY,
                VSOPBody.VENUS,
                VSOPBody.EARTH,
                VSOPBody.MARS,
                VSOPBody.JUPITER,
                VSOPBody.SATURN,
                VSOPBody.URANUS,
                VSOPBody.NEPTUNE,
            ],

            _ => []
        };

        /// <summary>
        /// Check body is in version or not.
        /// </summary>
        /// <param name="ver">Version enum</param>
        /// <param name="body">Planet enum</param>
        /// <returns>True if version contain this planet</returns>
        public static bool CheckAvailability(VSOPVersion ver, VSOPBody body) => ver switch
        {
            VSOPVersion.VSOP87 => body is VSOPBody.MERCURY or VSOPBody.VENUS or VSOPBody.MARS
                               or VSOPBody.JUPITER or VSOPBody.SATURN or VSOPBody.URANUS
                               or VSOPBody.NEPTUNE or VSOPBody.EMB,

            VSOPVersion.VSOP87A => body is VSOPBody.MERCURY or VSOPBody.VENUS or VSOPBody.EARTH
                                or VSOPBody.MARS or VSOPBody.JUPITER or VSOPBody.SATURN
                                or VSOPBody.URANUS or VSOPBody.NEPTUNE or VSOPBody.EMB,

            VSOPVersion.VSOP87B or VSOPVersion.VSOP87C or VSOPVersion.VSOP87D
                               => body is VSOPBody.MERCURY or VSOPBody.VENUS or VSOPBody.EARTH
                               or VSOPBody.MARS or VSOPBody.JUPITER or VSOPBody.SATURN
                               or VSOPBody.URANUS or VSOPBody.NEPTUNE,

            VSOPVersion.VSOP87E => body is VSOPBody.SUN or VSOPBody.MERCURY or VSOPBody.VENUS
                                or VSOPBody.EARTH or VSOPBody.MARS or VSOPBody.JUPITER
                                or VSOPBody.SATURN or VSOPBody.URANUS or VSOPBody.NEPTUNE,

            _ => false
        };

        /// <summary>
        /// Get coordinate type of VSOP87 version
        /// </summary>
        /// <param name="ver">Version enum</param>
        /// <returns>Coordinate type</returns>
        /// <exception cref="ArgumentException"></exception>
        public static CoordinatesType GetCoordinatesType(VSOPVersion ver) => ver switch
        {
            VSOPVersion.VSOP87 => CoordinatesType.Elliptic,
            VSOPVersion.VSOP87A or VSOPVersion.VSOP87C or VSOPVersion.VSOP87E => CoordinatesType.Rectangular,
            VSOPVersion.VSOP87B or VSOPVersion.VSOP87D => CoordinatesType.Spherical,
            _ => throw new ArgumentException()
        };

        /// <summary>
        /// Get Time Frame Reference of VSOP87 version
        /// </summary>
        /// <param name="ver">Version enum</param>
        /// <returns>Time Frame Reference</returns>
        public static FrameType GetFrameType(VSOPVersion ver) => ver switch
        {
            VSOPVersion.VSOP87E => FrameType.Barycentric,
            _ => FrameType.Dynamical
        };

        public static Epoch GetEpoch(VSOPVersion ver) => ver switch
        {
            VSOPVersion.VSOP87C or VSOPVersion.VSOP87D => Epoch.OfDate,
            _ => Epoch.J2000
        };

        public static double[,] MultiplyMatrix(double[,] A, double[,] B)
        {
            int rA = A.GetLength(0);
            int cA = A.GetLength(1);
            int rB = B.GetLength(0);
            int cB = B.GetLength(1);

            if (cA != rB)
            {
                throw new ArgumentException("Matrixes can't be multiplied!!");
            }
            else
            {
                double temp;
                double[,] kHasil = new double[rA, cB];

                for (int i = 0; i < rA; i++)
                {
                    for (int j = 0; j < cB; j++)
                    {
                        temp = 0;
                        for (int k = 0; k < cA; k++)
                        {
                            temp += A[i, k] * B[k, j];
                        }
                        kHasil[i, j] = temp;
                    }
                }

                return kHasil;
            }
        }

        /// <summary>
        /// Convert cartesian coordinate to spherical coordinate
        /// </summary>
        /// <param name="xyz">x,y,z,dx,dy,dz</param>
        /// <returns>l,b,r,dl,db,dr</returns>
        public static double[] XYZtoLBR(double[] xyz)
        {
            double[] lbr = new double[6];

            double x = xyz[0];
            double y = xyz[1];
            double z = xyz[2];
            double dx = xyz[3];
            double dy = xyz[4];
            double dz = xyz[5];
            double l, b, r, dl, db, dr;

            //r = sqrt(x^2+y^2+z^2)
            r = Math.Sqrt(x * x + y * y + z * z);

            //L = atan2(y, x)
            l = Math.Atan2(y, x);

            //θ = arccos(z/r)
            //b = pi/2 - θ.
            //Sin(θ) = Cos(b), Cos(θ) = Sin(b)
            b = Math.Asin(z / r);

            //modulo l into [0,Tau)
            l -= Math.Floor(l / Math.Tau) * Math.Tau;
            if (l < 0)
                l += Math.Tau;

            #region vector matrix mul

            if (Vector256.IsHardwareAccelerated)
            {
                double rho2 = x * x + y * y;
                double rho = Math.Sqrt(rho2);
                Vector256<double> v1 = Vector256.Create(x / r, y / r, z / r, 0);
                Vector256<double> v2 = Vector256.Create(x * z / (r * r * rho), y * z / (r * r * rho), -rho2 / (r * r * rho), 0);
                Vector256<double> v3 = Vector256.Create(-y / rho2, x / rho2, 0, 0);

                Vector256<double> vv = Vector256.Create(dx, dy, dz, 0);

                lbr[0] = l;
                lbr[1] = b;
                lbr[2] = r;
                lbr[3] = Vector256.Sum(v3 * vv);
                lbr[4] = -Vector256.Sum(v2 * vv);
                lbr[5] = Vector256.Sum(v1 * vv);
                return lbr;
            }

            #endregion vector matrix mul

            // Inverse Jacobian matrix  From  cartesian to spherical
            //https://en.wikipedia.org/wiki/Spherical_coordinate_system#Integration_and_differentiation_in_spherical_coordinates
            double[,] Inverse_J = {
                           {x/r                          , y/r                         , z/r                                 },
                           {x*z/(r*r*Math.Sqrt(x*x+y*y)) , y*z/(r*r*Math.Sqrt(x*x+y*y)),-(x*x+y*y)/(r*r*Math.Sqrt(x*x+y*y))  },
                           {-y/(x*x+y*y)                 , x/(x*x+y*y)                 , 0                                   }};
            double[,] Velocity = { { dx }, { dy }, { dz } };
            var C = MultiplyMatrix(Inverse_J, Velocity);
            dl = C[2, 0];
            db = C[1, 0];
            dr = C[0, 0];

            lbr[0] = l;
            lbr[1] = b;
            lbr[2] = r;
            lbr[3] = dl;
            lbr[4] = -db;
            lbr[5] = dr;
            return lbr;
        }

        /// <summary>
        /// convert spherical coordinate to cartesian coordinate
        /// </summary>
        /// <param name="lbr">l,b,r,dl,db,dr</param>
        /// <returns>x,y,z,dx,dy,dz</returns>
        public static double[] LBRtoXYZ(double[] lbr)
        {
            double[] xyz = new double[6];
            double x, y, z, dx, dy, dz;
            double l, b, r, dl, db, dr;

            l = lbr[0];
            b = lbr[1];
            r = lbr[2];
            dl = lbr[3];
            db = lbr[4];
            dr = lbr[5];

            x = r * Math.Cos(b) * Math.Cos(l);
            y = r * Math.Cos(b) * Math.Sin(l);
            z = r * Math.Sin(b);

            #region vector matrix mul

            if (Vector256.IsHardwareAccelerated)
            {
                Vector256<double> m1 = Vector256.Create(Math.Cos(b) * Math.Cos(l), r * Math.Sin(b) * Math.Cos(l), -r * Math.Cos(b) * Math.Sin(l), 0);
                Vector256<double> m2 = Vector256.Create(Math.Cos(b) * Math.Sin(l), r * Math.Sin(b) * Math.Sin(l), r * Math.Cos(b) * Math.Cos(l), 0);
                Vector256<double> m3 = Vector256.Create(Math.Sin(b), -r * Math.Cos(b), 0, 0);

                Vector256<double> vv = Vector256.Create(dr, db, dl, 0);

                xyz[0] = x;
                xyz[1] = y;
                xyz[2] = z;
                xyz[3] = Vector256.Sum(m1 * vv);
                xyz[4] = Vector256.Sum(m2 * vv);
                xyz[5] = -Vector256.Sum(m3 * vv);
                return xyz;
            }

            #endregion vector matrix mul

            // Jacobian matrix From spherical to cartesian
            //https://en.wikipedia.org/wiki/Spherical_coordinate_system#Integration_and_differentiation_in_spherical_coordinates
            double[,] J = {
                           { Math.Cos(b) * Math.Cos(l),  r * Math.Sin(b) * Math.Cos(l), -r * Math.Cos(b) * Math.Sin(l)  },
                           { Math.Cos(b) * Math.Sin(l),  r * Math.Sin(b) * Math.Sin(l),  r * Math.Cos(b) * Math.Cos(l)  },
                           { Math.Sin(b),               -r * Math.Cos(b),                0                              }};
            double[,] Velocity = { { dr }, { db }, { dl } };
            var C = MultiplyMatrix(J, Velocity);
            dx = C[0, 0];
            dy = C[1, 0];
            dz = C[2, 0];

            xyz[0] = x;
            xyz[1] = y;
            xyz[2] = z;
            xyz[3] = dx;
            xyz[4] = dy;
            xyz[5] = -dz;
            return xyz;
        }

        /// <summary>
        /// Convert Elliptic coordinate to cartesian coordinate.
        /// This is kind of magic that I will never understand.
        /// Directly translate from VSOP2013.f.
        /// </summary>
        /// <param name="body">planet</param>
        /// <param name="ell">Elliptic Elements: a,l,k,h,q,p </param>
        /// <returns>cartesian Heliocentric Coordinates</returns>
        public static double[] ELLtoXYZ(VSOPBody body, double[] ell)
        {
            double[] xyz = new double[6];
            double a, l, k, h, q, p;
            a = ell[0];
            l = ell[1];
            k = ell[2];
            h = ell[3];
            q = ell[4];
            p = ell[5];
            Complex z, z1, z2, z3, zto, zteta;
            double rgm, xfi, xki;
            double u, ex, ex2, ex3, gl, gm, e, dl, rsa;
            double xm, xr, xms, xmc, xn;

            //Initialization
            rgm = Math.Sqrt(gmp[(int)body] + gmp[0]);

            //Computation
            xfi = Math.Sqrt(1.0d - (k * k) - (h * h));
            xki = Math.Sqrt(1.0d - (q * q) - (p * p));
            u = 1.0d / (1.0d + xfi);
            z = new Complex(k, h);
            ex = z.Magnitude;
            ex2 = ex * ex;
            ex3 = ex * ex * ex;
            z1 = Complex.Conjugate(z);
            gl = ell[1] % (Math.Tau);
            gm = gl - Math.Atan2(h, k);
            e = gl + (ex - 0.125d * ex3) * Math.Sin(gm)
                + 0.5d * ex2 * Math.Sin(2.0d * gm)
                + 0.375d * ex3 * Math.Sin(3.0d * gm);
            while (true)
            {
                z2 = new Complex(0d, e);
                zteta = Complex.Exp(z2);
                z3 = z1 * zteta;
                dl = gl - e + z3.Imaginary;
                rsa = 1.0d - z3.Real;
                e += dl / rsa;
                if (Math.Abs(dl) < 1e-15) break;
            }

            z1 = u * z * z3.Imaginary;
            z2 = new Complex(z1.Imaginary, -z1.Real);
            zto = (-z + zteta + z2) / rsa;
            xm = p * zto.Real - q * zto.Imaginary;
            xr = a * rsa;

            xyz[0] = xr * (zto.Real - 2.0d * p * xm);
            xyz[1] = xr * (zto.Imaginary + 2.0d * q * xm);
            xyz[2] = -2.0d * xr * xki * xm;

            xms = a * (h + zto.Imaginary) / xfi;
            xmc = a * (k + zto.Real) / xfi;
            xn = rgm / (a * Math.Sqrt(a));

            xyz[3] = xn * ((2.0d * p * p - 1.0d) * xms + 2.0d * p * q * xmc);
            xyz[4] = xn * ((1.0d - 2.0d * q * q) * xmc - 2.0d * p * q * xms);
            xyz[5] = xn * 2.0d * xki * (p * xms + q * xmc);

            return xyz;
        }

        /// <summary>
        /// Convert Elliptic coordinate to spherical coordinate.
        /// </summary>
        /// <param name="body">planet</param>
        /// <returns>Spherical Heliocentric Coordinates</returns>
        public static double[] ELLtoLBR(VSOPBody body, double[] ell)
        {
            double[] xyz = ELLtoXYZ(body, ell);
            return XYZtoLBR(xyz);
        }

        /// <summary>
        /// Convert cartesian coordinate to Elliptic coordinate.
        /// Inverse of ELLtoXYZ.
        /// Powered By Claude Opus 4.6
        /// </summary>
        /// <param name="body">planet</param>
        /// <param name="xyz">cartesian Heliocentric Coordinates: x,y,z,dx,dy,dz</param>
        /// <returns>Elliptic Elements: a,l,k,h,q,p</returns>
        public static double[] XYZtoELL(VSOPBody body, double[] xyz)
        {
            double[] ell = new double[6];

            double x = xyz[0];
            double y = xyz[1];
            double z = xyz[2];
            double dx = xyz[3];
            double dy = xyz[4];
            double dz = xyz[5];

            double mu = gmp[(int)body] + gmp[0];

            // Distance and velocity squared
            double r = Math.Sqrt(x * x + y * y + z * z);
            double v2 = dx * dx + dy * dy + dz * dz;

            // Semi-major axis (vis-viva equation)
            double a = mu * r / (2.0d * mu - r * v2);

            // Angular momentum vector: h_vec = r_vec × v_vec
            double hvx = y * dz - z * dy;
            double hvy = z * dx - x * dz;
            double hvz = x * dy - y * dx;
            double hMag = Math.Sqrt(hvx * hvx + hvy * hvy + hvz * hvz);

            // Inclination components q, p using sin(i/2) convention
            // q = sin(i/2)cos(Ω), p = sin(i/2)sin(Ω)
            // Derived from: q = -h_y / (2|h|cos(i/2)), p = h_x / (2|h|cos(i/2))
            // where 2|h|cos(i/2) = sqrt(2|h|(|h|+h_z))
            double qpDenom = Math.Sqrt(2.0d * hMag * (hMag + hvz));
            double q = -hvy / qpDenom;
            double p = hvx / qpDenom;

            double xki = Math.Sqrt(1.0d - q * q - p * p);

            // Equinoctial frame unit vectors (from ELLtoXYZ rotation)
            // ê₁ and ê₂ span the orbital plane
            double e1x = 1.0d - 2.0d * p * p;
            double e1y = 2.0d * p * q;
            double e1z = -2.0d * p * xki;

            double e2x = 2.0d * p * q;
            double e2y = 1.0d - 2.0d * q * q;
            double e2z = 2.0d * q * xki;

            // Eccentricity vector: e_vec = ((v²-μ/r)·r_vec - (r·v)·v_vec) / μ
            double rdotv = x * dx + y * dy + z * dz;
            double coeff = v2 - mu / r;
            double ex = (coeff * x - rdotv * dx) / mu;
            double ey = (coeff * y - rdotv * dy) / mu;
            double ez = (coeff * z - rdotv * dz) / mu;

            // Eccentricity components by projection onto equinoctial frame
            double k = ex * e1x + ey * e1y + ez * e1z;
            double h = ex * e2x + ey * e2y + ez * e2z;

            double ecc = Math.Sqrt(k * k + h * h);
            double xfi = Math.Sqrt(1.0d - k * k - h * h);

            // True longitude F from position projection onto orbital plane
            double f = (x * e1x + y * e1y + z * e1z) / r;
            double g = (x * e2x + y * e2y + z * e2z) / r;
            double F = Math.Atan2(g, f);

            // Convert true longitude to eccentric longitude then mean longitude
            // ω̄ = atan2(h, k), ν = F - ω̄ (true anomaly)
            double omegaBar = Math.Atan2(h, k);
            double nu = F - omegaBar;

            // Eccentric anomaly E' from true anomaly ν (exact formula)
            double cosNu = Math.Cos(nu);
            double sinNu = Math.Sin(nu);
            double denomE = 1.0d + ecc * cosNu;
            double cosEp = (ecc + cosNu) / denomE;
            double sinEp = xfi * sinNu / denomE;
            double Ep = Math.Atan2(sinEp, cosEp);

            // Eccentric longitude E = E' + ω̄
            double E = Ep + omegaBar;

            // Mean longitude L from equinoctial Kepler equation: L = E - k·sin(E) + h·cos(E)
            double L = E - k * Math.Sin(E) + h * Math.Cos(E);

            ell[0] = a;
            ell[1] = L;
            ell[2] = k;
            ell[3] = h;
            ell[4] = q;
            ell[5] = p;

            return ell;
        }

        /// <summary>
        /// Convert spherical coordinate to Elliptic coordinate.
        /// </summary>
        /// <param name="body">planet</param>
        /// <param name="lbr">Spherical Heliocentric Coordinates: l,b,r,dl,db,dr</param>
        /// <returns>Elliptic Elements: a,l,k,h,q,p</returns>
        public static double[] LBRtoELL(VSOPBody body, double[] lbr)
        {
            double[] xyz = LBRtoXYZ(lbr);
            return XYZtoELL(body, xyz);
        }

        /// <summary>
        /// Convert heliocentric coordinates to barycentric coordinates
        /// by adding the Sun's barycentric position.
        /// </summary>
        /// <param name="helio">Heliocentric coordinates: x,y,z,dx,dy,dz</param>
        /// <param name="sunBary">Sun's barycentric coordinates: x,y,z,dx,dy,dz (from VSOP87E Sun)</param>
        /// <returns>Barycentric coordinates: x,y,z,dx,dy,dz</returns>
        public static double[] HelioToBarycentric(double[] helio, double[] sunBary)
        {
            double[] bary = new double[6];
            for (int i = 0; i < 6; i++)
            {
                bary[i] = helio[i] + sunBary[i];
            }
            return bary;
        }

        /// <summary>
        /// Convert barycentric coordinates to heliocentric coordinates
        /// by subtracting the Sun's barycentric position.
        /// </summary>
        /// <param name="bary">Barycentric coordinates: x,y,z,dx,dy,dz</param>
        /// <param name="sunBary">Sun's barycentric coordinates: x,y,z,dx,dy,dz (from VSOP87E Sun)</param>
        /// <returns>Heliocentric coordinates: x,y,z,dx,dy,dz</returns>
        public static double[] BarycentricToHelio(double[] bary, double[] sunBary)
        {
            double[] helio = new double[6];
            for (int i = 0; i < 6; i++)
            {
                helio[i] = bary[i] - sunBary[i];
            }
            return helio;
        }

        /// <summary>
        /// Convert inertial frame from dynamical to ICRS.
        /// </summary>
        /// <param name="dynamical">Ecliptic Heliocentric Coordinates - Dynamical Frame J2000</param>
        /// <returns>Equatorial Heliocentric Coordinates - ICRS Frame J2000</returns>
        public static double[] DynamicaltoICRS(double[] dynamical)
        {
            double[] icrs = new double[6];

            double dgrad = Math.PI / 180.0d;
            double sdrad = Math.PI / 180.0d / 3600.0d;
            double eps = (23.0d + 26.0d / 60.0d + 21.411360d / 3600.0d) * dgrad;
            double phi = -0.051880d * sdrad;

            double Seps, Ceps, Sphi, Cphi;
            Seps = Math.Sin(eps);
            Ceps = Math.Cos(eps);
            Sphi = Math.Sin(phi);
            Cphi = Math.Cos(phi);

            if (Vector256.IsHardwareAccelerated)
            {
                Vector256<double> r1 = Vector256.Create(Cphi, -Sphi * Ceps, Sphi * Seps, 0);
                Vector256<double> r2 = Vector256.Create(Sphi, Cphi * Ceps, -Cphi * Seps, 0);
                Vector256<double> r3 = Vector256.Create(0, Seps, Ceps, 0);

                Vector256<double> vv = Vector256.Create(dynamical[0], dynamical[1], dynamical[2], 0);
                Vector256<double> vdv = Vector256.Create(dynamical[3], dynamical[4], dynamical[5], 0);

                icrs[0] = Vector256.Sum(vv * r1);
                icrs[1] = Vector256.Sum(vv * r2);
                icrs[2] = Vector256.Sum(vv * r3);

                icrs[3] = Vector256.Sum(vdv * r1);
                icrs[4] = Vector256.Sum(vdv * r2);
                icrs[5] = Vector256.Sum(vdv * r3);
                return icrs;
            }

            //Rotation Matrix
            double[,] R = new double[,] { {Cphi, -Sphi*Ceps,  Sphi*Seps },
                                          {Sphi,  Cphi*Ceps, -Cphi*Seps },
                                          {0,     Seps,       Ceps      }};

            // Vector for cartesian coordinate element
            double[,] A = new double[,] { {dynamical[0]},
                                          {dynamical[1]},
                                          {dynamical[2]}};

            double[,] C = MultiplyMatrix(R, A);

            icrs[0] = C[0, 0];
            icrs[1] = C[1, 0];
            icrs[2] = C[2, 0];

            A = new double[,] { {dynamical[3]},
                                {dynamical[4]},
                                {dynamical[5]}};

            C = MultiplyMatrix(R, A);
            icrs[3] = C[0, 0];
            icrs[4] = C[1, 0];
            icrs[5] = C[2, 0];

            return icrs;
        }

        /// <summary>
        /// Convert inertial frame from ICRS to dynamical.
        /// </summary>
        /// <param name="icrs">Equatorial Heliocentric Coordinates - ICRS Frame J2000</param>
        /// <returns>Ecliptic Heliocentric Coordinates - Dynamical Frame J2000</returns>
        public static double[] ICRStoDynamical(double[] icrs)
        {
            double[] dynamical = new double[6];

            double dgrad = Math.PI / 180.0d;
            double sdrad = Math.PI / 180.0d / 3600.0d;
            double eps = (23.0d + 26.0d / 60.0d + 21.411360d / 3600.0d) * dgrad;
            double phi = -0.051880d * sdrad;

            double Seps, Ceps, Sphi, Cphi;
            Seps = Math.Sin(eps);
            Ceps = Math.Cos(eps);
            Sphi = Math.Sin(phi);
            Cphi = Math.Cos(phi);

            #region vector matrix mul

            if (Vector256.IsHardwareAccelerated)
            {
                Vector256<double> m1 = Vector256.Create(Cphi, Sphi, 0, 0);
                Vector256<double> m2 = Vector256.Create(-Sphi * Ceps, Cphi * Ceps, Seps, 0);
                Vector256<double> m3 = Vector256.Create(Sphi * Seps, -Cphi * Seps, Ceps, 0);

                Vector256<double> vv = Vector256.Create(icrs[0], icrs[1], icrs[2], 0);
                Vector256<double> vdv = Vector256.Create(icrs[3], icrs[4], icrs[5], 0);

                dynamical[0] = Vector256.Sum(vv * m1);
                dynamical[1] = Vector256.Sum(vv * m2);
                dynamical[2] = Vector256.Sum(vv * m3);

                dynamical[3] = Vector256.Sum(vdv * m1);
                dynamical[4] = Vector256.Sum(vdv * m2);
                dynamical[5] = Vector256.Sum(vdv * m3);
                return dynamical;
            }

            #endregion vector matrix mul

            //Reverse Matrix
            double[,] R_1 = new double[,] {{ Cphi,       Sphi,      0    },
                                           {-Sphi*Ceps,  Cphi*Ceps, Seps },
                                           { Sphi*Seps, -Cphi*Seps, Ceps }};
            // Vector for cartesian coordinate element
            double[,] A = new double[,] { {icrs[0]},
                                          {icrs[1]},
                                          {icrs[2]}};

            double[,] C = MultiplyMatrix(R_1, A);

            dynamical[0] = C[0, 0];
            dynamical[1] = C[1, 0];
            dynamical[2] = C[2, 0];

            A = new double[,] { {icrs[3]},
                                {icrs[4]},
                                {icrs[5]}};

            C = MultiplyMatrix(R_1, A);
            dynamical[3] = C[0, 0];
            dynamical[4] = C[1, 0];
            dynamical[5] = C[2, 0];

            return dynamical;
        }

        /// <summary>
        /// Apply IAU 1976 luni-solar precession to rotate coordinates
        /// from the fixed J2000 ecliptic frame to the ecliptic frame of a given date.
        /// <para>
        /// The three precession angles (Lieske et al. 1977) are polynomials in T
        /// (Julian centuries from J2000), measured in arcseconds:
        /// <list type="bullet">
        ///   <item>ζ_A = 2306.2181·T + 0.30188·T² + 0.017998·T³</item>
        ///   <item>z_A = 2306.2181·T + 1.09468·T² + 0.018203·T³</item>
        ///   <item>θ_A = 2004.3109·T − 0.42665·T² − 0.041775·T³</item>
        /// </list>
        /// The rotation applied is  P = R_z(−z_A) · R_y(θ_A) · R_z(−ζ_A).
        /// </para>
        /// </summary>
        /// <param name="xyz">Ecliptic coordinates in J2000 frame: x,y,z,dx,dy,dz</param>
        /// <param name="time">Observation time (used to compute T)</param>
        /// <returns>Ecliptic coordinates in the frame of the given date: x,y,z,dx,dy,dz</returns>
        public static double[] J2000toDate(double[] xyz, VSOPTime time)
        {
            double[] ofdate = new double[6];

            // Julian centuries from J2000.0
            double T = (time.JulianDate - 2451545.0d) / 36525.0d;

            // Precession angles in arcseconds → radians
            double arcsec = Math.PI / 648000.0d;  // π / (180 × 3600)
            double zetaA = (2306.2181d * T + 0.30188d * T * T + 0.017998d * T * T * T) * arcsec;
            double zA = (2306.2181d * T + 1.09468d * T * T + 0.018203d * T * T * T) * arcsec;
            double thetaA = (2004.3109d * T - 0.42665d * T * T - 0.041775d * T * T * T) * arcsec;

            double Szeta = Math.Sin(zetaA), Czeta = Math.Cos(zetaA);
            double Sz = Math.Sin(zA), Cz = Math.Cos(zA);
            double Stheta = Math.Sin(thetaA), Ctheta = Math.Cos(thetaA);

            // Precession matrix P = R_z(-zA) · R_y(θA) · R_z(-ζA)
            // Row vectors of P (each row dotted with the input vector gives one output component):
            //   P[0] = [ Cz·Ctheta·Czeta - Sz·Szeta,   Cz·Ctheta·Szeta + Sz·Czeta,   Cz·Stheta ]
            //   P[1] = [-Sz·Ctheta·Czeta - Cz·Szeta,  -Sz·Ctheta·Szeta + Cz·Czeta,  -Sz·Stheta ]
            //   P[2] = [         -Stheta·Czeta,                  -Stheta·Szeta,          Ctheta  ]

            if (Vector256.IsHardwareAccelerated)
            {
                Vector256<double> row0 = Vector256.Create(Cz * Ctheta * Czeta - Sz * Szeta,
                                                           Cz * Ctheta * Szeta + Sz * Czeta,
                                                           Cz * Stheta, 0);
                Vector256<double> row1 = Vector256.Create(-Sz * Ctheta * Czeta - Cz * Szeta,
                                                          -Sz * Ctheta * Szeta + Cz * Czeta,
                                                          -Sz * Stheta, 0);
                Vector256<double> row2 = Vector256.Create(-Stheta * Czeta,
                                                          -Stheta * Szeta,
                                                           Ctheta, 0);

                Vector256<double> pos = Vector256.Create(xyz[0], xyz[1], xyz[2], 0);
                Vector256<double> vel = Vector256.Create(xyz[3], xyz[4], xyz[5], 0);

                ofdate[0] = Vector256.Sum(row0 * pos);
                ofdate[1] = Vector256.Sum(row1 * pos);
                ofdate[2] = Vector256.Sum(row2 * pos);
                ofdate[3] = Vector256.Sum(row0 * vel);
                ofdate[4] = Vector256.Sum(row1 * vel);
                ofdate[5] = Vector256.Sum(row2 * vel);
                return ofdate;
            }

            double[,] P = new double[,]
            {
                {  Cz * Ctheta * Czeta - Sz * Szeta,   Cz * Ctheta * Szeta + Sz * Czeta,   Cz * Stheta },
                { -Sz * Ctheta * Czeta - Cz * Szeta,  -Sz * Ctheta * Szeta + Cz * Czeta,  -Sz * Stheta },
                {         -Stheta * Czeta,                      -Stheta * Szeta,                Ctheta  }
            };

            double[,] pos3 = { { xyz[0] }, { xyz[1] }, { xyz[2] } };
            double[,] vel3 = { { xyz[3] }, { xyz[4] }, { xyz[5] } };

            double[,] rPos = MultiplyMatrix(P, pos3);
            double[,] rVel = MultiplyMatrix(P, vel3);

            ofdate[0] = rPos[0, 0]; ofdate[1] = rPos[1, 0]; ofdate[2] = rPos[2, 0];
            ofdate[3] = rVel[0, 0]; ofdate[4] = rVel[1, 0]; ofdate[5] = rVel[2, 0];
            return ofdate;
        }

        /// <summary>
        /// Apply the inverse of the IAU 1976 precession to rotate coordinates
        /// from the ecliptic frame of a given date back to the fixed J2000 ecliptic frame.
        /// <para>
        /// Because P is a rotation matrix, its inverse equals its transpose P^T =
        /// R_z(ζ_A) · R_y(−θ_A) · R_z(z_A).
        /// </para>
        /// </summary>
        /// <param name="xyz">Ecliptic coordinates in the frame of the given date: x,y,z,dx,dy,dz</param>
        /// <param name="time">Observation time (used to compute T)</param>
        /// <returns>Ecliptic coordinates in J2000 frame: x,y,z,dx,dy,dz</returns>
        public static double[] DatetoJ2000(double[] xyz, VSOPTime time)
        {
            double[] j2000 = new double[6];

            // Julian centuries from J2000.0
            double T = (time.JulianDate - 2451545.0d) / 36525.0d;

            // Precession angles in arcseconds → radians
            double arcsec = Math.PI / 648000.0d;  // π / (180 × 3600)
            double zetaA = (2306.2181d * T + 0.30188d * T * T + 0.017998d * T * T * T) * arcsec;
            double zA = (2306.2181d * T + 1.09468d * T * T + 0.018203d * T * T * T) * arcsec;
            double thetaA = (2004.3109d * T - 0.42665d * T * T - 0.041775d * T * T * T) * arcsec;

            double Szeta = Math.Sin(zetaA), Czeta = Math.Cos(zetaA);
            double Sz = Math.Sin(zA), Cz = Math.Cos(zA);
            double Stheta = Math.Sin(thetaA), Ctheta = Math.Cos(thetaA);

            // Inverse precession matrix P^T  (transpose of P used in J2000toDate)
            // Row vectors of P^T (columns of P):
            //   P^T[0] = [  Cz·Ctheta·Czeta - Sz·Szeta,  -Sz·Ctheta·Czeta - Cz·Szeta,  -Stheta·Czeta ]
            //   P^T[1] = [  Cz·Ctheta·Szeta + Sz·Czeta,  -Sz·Ctheta·Szeta + Cz·Czeta,  -Stheta·Szeta ]
            //   P^T[2] = [              Cz·Stheta,                    -Sz·Stheta,              Ctheta  ]

            if (Vector256.IsHardwareAccelerated)
            {
                Vector256<double> row0 = Vector256.Create(Cz * Ctheta * Czeta - Sz * Szeta,
                                                          -Sz * Ctheta * Czeta - Cz * Szeta,
                                                          -Stheta * Czeta, 0);
                Vector256<double> row1 = Vector256.Create(Cz * Ctheta * Szeta + Sz * Czeta,
                                                          -Sz * Ctheta * Szeta + Cz * Czeta,
                                                          -Stheta * Szeta, 0);
                Vector256<double> row2 = Vector256.Create(Cz * Stheta,
                                                         -Sz * Stheta,
                                                          Ctheta, 0);

                Vector256<double> pos = Vector256.Create(xyz[0], xyz[1], xyz[2], 0);
                Vector256<double> vel = Vector256.Create(xyz[3], xyz[4], xyz[5], 0);

                j2000[0] = Vector256.Sum(row0 * pos);
                j2000[1] = Vector256.Sum(row1 * pos);
                j2000[2] = Vector256.Sum(row2 * pos);
                j2000[3] = Vector256.Sum(row0 * vel);
                j2000[4] = Vector256.Sum(row1 * vel);
                j2000[5] = Vector256.Sum(row2 * vel);
                return j2000;
            }

            double[,] PT = new double[,]
            {
                {  Cz * Ctheta * Czeta - Sz * Szeta,  -Sz * Ctheta * Czeta - Cz * Szeta,  -Stheta * Czeta },
                {  Cz * Ctheta * Szeta + Sz * Czeta,  -Sz * Ctheta * Szeta + Cz * Czeta,  -Stheta * Szeta },
                {              Cz * Stheta,                        -Sz * Stheta,                 Ctheta    }
            };

            double[,] pos3 = { { xyz[0] }, { xyz[1] }, { xyz[2] } };
            double[,] vel3 = { { xyz[3] }, { xyz[4] }, { xyz[5] } };

            double[,] rPos = MultiplyMatrix(PT, pos3);
            double[,] rVel = MultiplyMatrix(PT, vel3);

            j2000[0] = rPos[0, 0]; j2000[1] = rPos[1, 0]; j2000[2] = rPos[2, 0];
            j2000[3] = rVel[0, 0]; j2000[4] = rVel[1, 0]; j2000[5] = rVel[2, 0];
            return j2000;
        }
    }
}