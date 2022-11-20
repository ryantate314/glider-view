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
                ReleaseHeightMeters = release?.GpsAltitude,
                MaxAltitudeMeters = data.Max(x => x.GpsAltitude),
            };

            if (release != null)
            {
                var flightAfterRelease = data.Where(x => x.Time > release.Time)
                    .ToList();
                stats.DistanceTraveledKm = (float)FindDistance(flightAfterRelease);

                stats.AltitudeGainedMeters = FindAltitudeGained(flightAfterRelease);
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

        private double FindDistance(List<Waypoint> waypoints)
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

        double GetDistanceFromLatLonInKm(double lat1, double lon1, double lat2, double lon2)
        {
            const int R = 6371; // Radius of the earth in km
            var dLat = Deg2Rad(lat2 - lat1);  // deg2rad below
            var dLon = Deg2Rad(lon2 - lon1);
            var a =
              Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
              Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) *
              Math.Sin(dLon / 2) * Math.Sin(dLon / 2)
              ;
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c; // Distance in km
            return d;
        }

        double Deg2Rad(double deg)
        {
            const double piRad = Math.PI / 180;
            return deg * piRad;
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
    }
}
