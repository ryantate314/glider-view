using GeoLibrary.Model;
using GliderView.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service
{
    public class FlightAnalyzer
    {

        private readonly static Polygon Pattern = new Polygon(new Point[]
        {
            new Point(-84.573683, 35.226593),
            new Point(-84.591593, 35.235328),
            new Point(-84.579051, 35.219070),
            new Point(-84.596894, 35.227574)
        });
        
        public FlightStatistics Analyze(Flight flight)
        {
            if (flight.Waypoints == null)
                throw new ArgumentNullException("Flight.Waypoints");

            var waypoints = flight.Waypoints.OrderBy(x => x.Time)
                .ToList();

            List<Waypoint> data = SmoothData(waypoints);

            Waypoint? release = FindReleasePoint(data);

            var stats = new FlightStatistics()
            {
                ReleaseHeight = release?.GpsAltitude,
                MaxAltitude = data.Max(x => x.GpsAltitude),
            };

            if (release != null)
            {
                var flightAfterRelease = data.Where(x => x.Time > release.Time)
                    .ToList();
                stats.DistanceTraveled = (float)FindDistance(flightAfterRelease);

                stats.AltitudeGained = FindAltitudeGained(flightAfterRelease);
            }

            stats.PatternEntryAltitude = GetPatternEntry(waypoints)?.GpsAltitude;

            return stats;
        }

        private int FindAltitudeGained(List<Waypoint> waypoints)
        {
            int altitudeGained = 0;
            for (int i = 1; i < waypoints.Count; i++)
            {
                var lastWaypoint = waypoints[i - 1];
                var waypoint = waypoints[i];

                if (waypoint.GpsAltitude > lastWaypoint.GpsAltitude)
                    altitudeGained += waypoint.GpsAltitude - lastWaypoint.GpsAltitude;
            }
            return altitudeGained;
        }

        private static double FindDistance(List<Waypoint> waypoints)
        {
            double distance = 0;

            for (int i = 1; i < waypoints.Count; i++)
            {
                var lat1 = waypoints[i - 1].Latitude;
                var lon1 = waypoints[i - 1].Longitude;

                var lat2 = waypoints[i].Latitude;
                var lon2 = waypoints[i].Longitude;

                distance += GetDistanceFromLatLonInKm(lat1, lon1, lat2, lon2);
            }

            return distance;
        }

        private static double GetDistanceFromLatLonInKm(Point p1, Point p2)
        {
            return p1.HaversineDistanceTo(p2);
        }

        private static double GetDistanceFromLatLonInKm(double lat1, double lon1, double lat2, double lon2)
        {
            return GetDistanceFromLatLonInKm(
                new Point(lon1, lat1),
                new Point(lon2, lat2)
            );
        }

        private static double Deg2Rad(double deg)
        {
            const double piRad = Math.PI / 180;
            return deg * piRad;
        }

        private static double Rad2Deg(double rad)
        {
            const double piDeg = 180 / Math.PI;
            return rad * piDeg;
        }
        
        private Waypoint? FindReleasePoint(List<Waypoint> waypoints)
        {
            if (waypoints.Count < 10)
                throw new ArgumentException("Not enough waypoints to analyze.");

            Waypoint lastWaypoint = waypoints[0];
            Waypoint? suspectedRelease = null;
            for (int i = 1; i < waypoints.Count; i++)
            {
                var waypoint = waypoints[i];

                if (waypoint.GpsAltitude < lastWaypoint.GpsAltitude)
                {
                    // If we've found 2 descending points in a row
                    if (suspectedRelease != null)
                    {
                        break;
                    }
                    suspectedRelease = waypoint;
                }
                else
                {
                    suspectedRelease = null;
                }

                lastWaypoint = waypoint;
            }

            return suspectedRelease;
        }

        private List<Waypoint> SmoothData(List<Waypoint> waypoints)
        {

            var output = new List<Waypoint>();
            var altBuffer = new Waypoint?[3];

            for (int i = 0; i < waypoints.Count(); i++)
            {
                var waypoint = waypoints[i];
                // 1 2 3 4 5 6

                // n=3;i=0 -> 2
                // n=3;i=1 -> 3
                // n=3;i=m-1 -> 2

                // n=5;i=0 -> 3
                // n=5;i=1 -> 4
                // n=5;i=2 -> 5
                altBuffer[0] = null;
                altBuffer[1] = null;
                altBuffer[2] = null;

                if (i > 0)
                    altBuffer[0] = waypoints[i - 1];
                altBuffer[1] = waypoint;
                if (i < waypoints.Count - 1)
                    altBuffer[2] = waypoints[i + 1];

                var buffer = altBuffer.Where(x => x != null)
                    .ToList();

                output.Add(new Waypoint()
                {
                    Time = waypoint.Time,
                    Longitude = waypoint.Longitude,
                    Latitude = waypoint.Latitude,
                    GpsAltitude = (int)Math.Round(buffer.Average(x => x.GpsAltitude))
                });
            }

            return output;
        }

        private Waypoint? GetPatternEntry(List<Waypoint> waypoints)
        {
            // Work backwards from landing

            foreach (var waypoint in waypoints.Reverse<Waypoint>())
            {
                if (!Pattern.IsPointInside(new Point(waypoint.Longitude, waypoint.Latitude)))
                {
                    return waypoint;
                }
            }

            return null;
        }

        private double GetBearing(double lat1, double lon1, double lat2, double lon2)
        {
            double length = GetDistanceFromLatLonInKm(lat1, lon1, lat2, lon2);

            double x = Math.Cos(lat2) * Math.Sin(length);
            //cos θa * sin θb – sin θa * cos θb * cos ∆L
            double y = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(length);

            double bearingRad = Math.Atan2(x, y);

            return Rad2Deg(bearingRad);
        }
    }
}
