using GeoLibrary.Model;
using GliderView.Service.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service
{
    public interface IFlightAnalyzer
    {
        FlightStatistics Analyze(Flight flight);
    }

    public class FlightAnalyzer : IFlightAnalyzer
    {
        public FlightAnalyzer(ILogger<FlightAnalyzer> logger)
        {
            _logger = logger;
        }

        private readonly static Polygon Pattern = new Polygon(new Point[]
        {
            // NE
            new Point(-84.573683, 35.226593),
            // NW
            new Point(-84.591593, 35.235328),
            // SW
            new Point(-84.596894, 35.227574),
            // SE
            new Point(-84.579051, 35.219070),
            // NE - Repeat first point to close polygon
            new Point(-84.573683, 35.226593)
        });
        private readonly ILogger<FlightAnalyzer> _logger;

        public FlightStatistics Analyze(Flight flight)
        {
            if (flight.Waypoints == null)
                throw new ArgumentNullException("Flight.Waypoints");

            var waypoints = flight.Waypoints.OrderBy(x => x.Time)
                .ToList();

            // Reset flight events
            foreach (var waypoint in waypoints)
                waypoint.FlightEvent = null;

            // Disable smoothing temporarily
            List<Waypoint> data = waypoints; // SmoothData(waypoints);

            Waypoint? release = FindReleasePoint(data);
            if (release != null)
                // Find original waypoint because SmoothData() performs a clone
                //waypoints.First(x => x.WaypointId == release.WaypointId)
                    //.FlightEvent = FlightEventType.Release;
                release.FlightEvent = FlightEventType.Release;

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

            Waypoint? patternEntry = GetPatternEntry(waypoints);
            if (patternEntry != null)
            {
                patternEntry.FlightEvent = FlightEventType.PatternEntry;
                stats.PatternEntryAltitude = patternEntry.GpsAltitude;
            }

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
            {
                _logger.LogWarning("Unable to find release point due to not enough waypoints.");
                return null;
            }

            const int skip = 3;
            for (int i = skip - 1; i < waypoints.Count - 2; i++)
            {
                var waypoint = waypoints[i];
                var nextWaypoint = waypoints[i + 1];
                var thirdWaypoint = waypoints[i + 2];

                // If we have 2 descending waypoints in a row
                if (thirdWaypoint.GpsAltitude < nextWaypoint.GpsAltitude && nextWaypoint.GpsAltitude < waypoint.GpsAltitude)
                {
                    return waypoints[i];
                }
            }

            return null;
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
                    WaypointId = waypoint.WaypointId,
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

            if (waypoints.Count < 10)
            {
                _logger.LogWarning("Unable to calculate pattern entry due to not enough waypoints.");
                return null;
            }

            var reverseWaypoints = waypoints.Reverse<Waypoint>()
                .ToList();

            // FINAL
            // Get avearage bearing for last few waypoints
            // Skip waypoints used to clear the runway
            const int skipWaypoints = 3;
            const int numFinalWaypoints = 3;
            double bearingSum = 0;
            for (int i = skipWaypoints + 1; i < skipWaypoints + numFinalWaypoints; i++)
            {
                bearingSum += GetBearing(
                    // Swap the direction since we are moving backwards through the waypoints
                    reverseWaypoints[i].Latitude,
                    reverseWaypoints[i].Longitude,
                    reverseWaypoints[i - 1].Latitude,
                    reverseWaypoints[i - 1].Longitude
                );
            }
            double finalBearing = bearingSum / (numFinalWaypoints - 1);

            // BASE / DOWNWIND
            const double bearingThreshold = 15;

            // Back-azimuth of final approach
            int downwindBearing = (int)Math.Round(Math.Abs((finalBearing + 180) % 360));

            bool downwindEstablished = false;
            for (int i = numFinalWaypoints + 1; i < waypoints.Count; i++)
            {
                double bearing = GetBearing(
                    // Swap the direction since we are moving backwards through the waypoints
                    reverseWaypoints[i].Latitude,
                    reverseWaypoints[i].Longitude,
                    reverseWaypoints[i - 1].Latitude,
                    reverseWaypoints[i - 1].Longitude
                );

                if (!downwindEstablished && Math.Abs(bearing - downwindBearing) < bearingThreshold)
                {
                    downwindEstablished |= true;
                    i += 3; // Skip a few waypoints to get established on downwind
                }
                else if (downwindEstablished && Math.Abs(bearing - downwindBearing) > bearingThreshold)
                    return reverseWaypoints[i - 1];
                else if (downwindEstablished && !Pattern.IsPointInside(new Point(reverseWaypoints[i].Longitude, reverseWaypoints[i].Latitude)))
                    return reverseWaypoints[i - 1];
            }

            return null;
        }

        private double GetBearing(double lat1, double lon1, double lat2, double lon2)
        {
            double length = GetDistanceFromLatLonInKm(lat1, lon1, lat2, lon2);

            //double x = Math.Cos(Deg2Rad(lat2)) * Math.Sin(length);
            ////cos θa * sin θb – sin θa * cos θb * cos ∆L
            //double y = Math.Cos(Deg2Rad(lat1)) * Math.Sin(Deg2Rad(lat2)) - Math.Sin(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) * Math.Cos(length);

            double x = Math.Sin(Deg2Rad(lon2 - lon1)) * Math.Cos(Deg2Rad(lat2));
            double y = Math.Cos(Deg2Rad(lat1)) * Math.Sin(Deg2Rad(lat2))
                - Math.Sin(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) * Math.Cos(Deg2Rad(lon2 - lon1));

            double bearingRad = Math.Atan2(x, y);

            double degrees = Rad2Deg(bearingRad);

            if (degrees < 0)
                degrees += 360;

            return degrees;
        }
    }
}
