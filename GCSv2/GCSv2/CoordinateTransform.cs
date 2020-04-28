using System;

namespace GCSv2
{
    class CoordinateTransform
    {
        //不變常數
        public const double a = 6378137.0000;
        public const double b = 6356752.3142;
        public static double e = Math.Sqrt(1.0 - Math.Pow((b / a), 2));

        //public static double[] xyz = new double[3];
        public static double[] orgxyz = new double[3];
        public static double[] orgllh = new double[3];

        //llh座標轉ecef座標
        public static double[] llh2xyz(double lat, double lon, double h)
        {
            double latRad = lat * Math.PI / 180.0;
            double lonRad = lon * Math.PI / 180.0;

            var sinlat = Math.Sin(latRad);
            var coslat = Math.Cos(latRad);
            var sinlon = Math.Sin(lonRad);
            var coslon = Math.Cos(lonRad);
            var tan2lat = Math.Pow(Math.Tan(latRad), 2);
            var tmp = 1 - Math.Pow(e, 2);
            var tmpden = Math.Sqrt(1 + tmp * tan2lat);

            var x = (a * coslon) / tmpden + h * coslon * coslat;
            var y = (a * sinlon) / tmpden + h * sinlon * coslat;

            var tmp2 = Math.Sqrt(1 - e * e * sinlat * sinlat);
            var z = (a * tmp * sinlat) / tmp2 + h * sinlat;

            double[] xyz = new double[3];

            xyz[0] = x;
            xyz[1] = y;
            xyz[2] = z;

            return xyz;

        }

        //ecef座標轉enu座標
        public static double[] xyz2enu(double x, double y, double z)
        {
            //原點xyz座標
            orgxyz[0] = -2956971.81;
            orgxyz[1] = 5075827.43;
            orgxyz[2] = 2476273.60;
            //原點llh座標
            orgllh[0] = 22.995528;
            orgllh[1] = 120.223348;
            orgllh[2] = 26.00;

            double[] tmpxyz = new double[3];
            double[] tmporg = orgxyz;

            //目標點到原點距離
            double[] difxyz = new double[3];
            difxyz[0] = tmpxyz[0] - tmporg[0];
            difxyz[1] = tmpxyz[1] - tmporg[1];
            difxyz[2] = tmpxyz[2] - tmporg[2];

            double phi = orgllh[0] * Math.PI / 180;
            double lam = orgllh[1] * Math.PI / 180;
            double sinphi = Math.Sin(phi);
            double cosphi = Math.Cos(phi);
            double sinlam = Math.Sin(lam);
            double coslam = Math.Cos(lam);

            double[,] R = new double[3, 3] { { -sinlam         , coslam          , 0 },
                                             { -sinphi * coslam, -sinphi * sinlam, cosphi },
                                             { -sinphi * coslam, cosphi * sinlam , sinphi } };
            double[] enu = new double[3];

            enu[0] = R[0, 0] * difxyz[0] + R[0, 1] * difxyz[1] + R[0, 2] * difxyz[2];
            enu[1] = R[1, 0] * difxyz[0] + R[1, 1] * difxyz[1] + R[1, 2] * difxyz[2];
            enu[2] = R[2, 0] * difxyz[0] + R[2, 1] * difxyz[1] + R[2, 2] * difxyz[2];

            return (enu);
        }

        //llh座標轉ned座標
        public static double[] llh2ned(double lat, double lon, double h)
        {
            double[] xyz = llh2xyz(lat, lon, h);
            double[] enu = xyz2enu(xyz[0], xyz[1], xyz[2]);
            double[] ned = new double[3] { enu[1], enu[0], -enu[2] };
            return (ned);
        }

        //ned座標轉ecef座標
        public static double[] ned2xyz(double n, double e, double d)
        {
            //原點xyz座標
            orgxyz[0] = -2956971.81;
            orgxyz[1] = 5075827.43;
            orgxyz[2] = 2476273.60;
            //原點llh座標
            orgllh[0] = 22.995528;
            orgllh[1] = 120.223348;
            orgllh[2] = 26.00;

            double phi = orgllh[0] * Math.PI / 180;
            double lam = orgllh[1] * Math.PI / 180;
            double sinphi = Math.Sin(phi);
            double cosphi = Math.Cos(phi);
            double sinlam = Math.Sin(lam);
            double coslam = Math.Cos(lam);

            double[,] invR = new double[3, 3] { { -sinlam, -sinphi * coslam, cosphi * coslam },
                                                { coslam , -sinphi * sinlam, cosphi * sinlam },
                                                { 0      , cosphi          , sinphi } };

            double[] xyz = new double[3];

            xyz[0] = invR[0, 0] * e + invR[0, 1] * n + invR[0, 2] * (-d) + orgxyz[0];
            xyz[1] = invR[1, 0] * e + invR[1, 1] * n + invR[1, 2] * (-d) + orgxyz[1];
            xyz[2] = invR[2, 0] * e + invR[2, 1] * n + invR[2, 2] * (-d) + orgxyz[2];

            return (xyz);
        }

        //ecef座標轉llh座標
        public static double[] xyz2llh(double x, double y, double z)
        {
            double x2 = x * x;
            double y2 = y * y;
            double z2 = z * z;
            double b2 = b * b;
            double e2 = e * e;
            double ep = e * (a / b);
            double r = Math.Sqrt(x2 + y2);
            double r2 = r * r;
            double E2 = a * a - b * b;
            double F = 54 * b2 * z * z;
            double G = r2 + (1 - e2) * z2 - e2 * E2;
            double c = (e2 * e2 * F * r2) / (G * G * G);
            double s = Math.Pow((1 + c + Math.Sqrt(c * c + 2 * c)), (1 / 3));
            double P = F / (3 * Math.Pow((s + 1 / s + 1), 2) * G * G);
            double Q = Math.Sqrt(1 + 2 * e2 * e2 * P);
            double ro = -(P * e2 * r) / (1 + Q) + Math.Sqrt((a * a / 2) * (1 + 1 / Q) - (P * (1 - e2) * z2) / (Q * (1 + Q)) - P * r2 / 2);

            double tmp = Math.Pow((r - e2 * ro), 2);
            double U = Math.Sqrt(tmp + z2);
            double V = Math.Sqrt(tmp + (1 - e2) * z2);
            double zo = (b2 * z) / (a * V);

            double height = U * (1 - b2 / (a * V));
            double lat = Math.Atan((z + ep * ep * zo) / r) / Math.PI * 180;

            double temp = Math.Atan(y / x);

            double lon;
            if (x >= 0)
                lon = temp / Math.PI * 180;
            else if (x < 0 && y >= 0)
                lon = (Math.PI + temp) / Math.PI * 180;
            else
                lon = (temp - Math.PI) / Math.PI * 180;

            double[] llh = new double[3];
            llh[0] = lat;
            llh[1] = lon;
            llh[2] = height;

            return (llh);
        }

        //計算碰撞點
        public static double[] CalculateIntersection(double x1, double y1, double m1, double x2, double y2, double m2)
        {
            double[] inter = new double[2];
            inter[0] = ((y2 - y1 + m1 * x1 - m2 * x2) / (m1 - m2));
            inter[1] = ((m1 * m2 * (x2 - x1) + m2 * y1 - m1 * y2) / (m2 - m1));
            return (inter);
        }
    }
}
