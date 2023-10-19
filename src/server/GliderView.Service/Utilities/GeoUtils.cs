using GeoLibrary.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace GliderView.Service.Utilities
{
    public static class GeoUtils
    {
        public static double GetBearing(double lat1, double lon1, double lat2, double lon2)
        {
            double length = GetDistanceFromLatLonInKm(lat1, lon1, lat2, lon2);

            double x = Math.Sin(Deg2Rad(lon2 - lon1)) * Math.Cos(Deg2Rad(lat2));
            double y = Math.Cos(Deg2Rad(lat1)) * Math.Sin(Deg2Rad(lat2))
                - Math.Sin(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) * Math.Cos(Deg2Rad(lon2 - lon1));

            double bearingRad = Math.Atan2(x, y);

            double degrees = Rad2Deg(bearingRad);

            if (degrees < 0)
                degrees += 360;

            return degrees;
        }

        public static double Deg2Rad(double deg)
        {
            const double piRad = Math.PI / 180;
            return deg * piRad;
        }

        public static double Rad2Deg(double rad)
        {
            const double piDeg = 180 / Math.PI;
            return rad * piDeg;
        }

        public static double GetDistanceFromLatLonInKm(Point p1, Point p2)
        {
            return p1.HaversineDistanceTo(p2);
        }

        public static double GetDistanceFromLatLonInKm(double lat1, double lon1, double lat2, double lon2)
        {
            return GetDistanceFromLatLonInKm(
                new Point(lon1, lat1),
                new Point(lon2, lat2)
            );
        }
    }
}
