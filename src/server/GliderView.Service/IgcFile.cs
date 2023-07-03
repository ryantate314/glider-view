using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service
{
    

    public class IgcFile
    {
        public class Waypoint
        {
            public DateTime Time { get; set; }
            public int GpsAltitude { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

        /// <summary>
        /// The aircraft model name, e.g. PW-6
        /// </summary>
        public string GliderType { get; set; }
        /// <summary>
        /// The aircraft's registration number, i.e. N-number.
        /// </summary>
        public string GliderId { get; set; }

        public DateTime DateOfFlight { get; set; }

        public string? ContestId { get; set; }

        public List<Waypoint> Waypoints { get; set; } = new List<Waypoint>();

        public static IgcFile Parse(Stream fileStream)
        {
            const string GLIDER_TYPE = "HFGTYGLIDERTYPE";
            const string GLIDER_ID = "HFGIDGLIDERID";
            const string DATE = "HFDTE";
            const string CONTEST_ID = "HFCIDCOMPETITIONID";

            var parsedFile = new IgcFile();

            fileStream.Seek(0, SeekOrigin.Begin);

            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, leaveOpen: true))
            {
                string? line;
                // Keep track of the previous timestamp to detect when the waypoints jump across midnight UTC time
                DateTime? previousTimestamp = null;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (line.StartsWith("B") && previousTimestamp != null)
                    {
                        Waypoint waypoint = ParseWaypoint(previousTimestamp.Value, line);
                        previousTimestamp = waypoint.Time;

                        parsedFile.Waypoints.Add(
                            waypoint
                        );
                    }
                    else if (line.StartsWith(GLIDER_TYPE))
                    {
                        parsedFile.GliderType = line.Split(':')[1];
                    }
                    else if (line.StartsWith(GLIDER_ID))
                    {
                        parsedFile.GliderId = line.Split(":")[1];
                    }
                    else if (line.StartsWith(DATE))
                    {
                        // ddMMyy
                        string date = line.Substring(DATE.Length, 6);
                        int day = Int32.Parse(date.Substring(0, 2));
                        int month = Int32.Parse(date.Substring(2, 2));

                        int year = 2000 + Int32.Parse(date.Substring(4, 2));

                        // The HFDTE header is the date the aircraft LANDED in UTC
                        parsedFile.DateOfFlight = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);

                        previousTimestamp = parsedFile.DateOfFlight;
                    }
                    else if (line.StartsWith(CONTEST_ID))
                    {
                        string contestId = line.Split(':')[1];
                        parsedFile.ContestId = contestId == String.Empty ? null : contestId;
                    }
                }
            }

            if (parsedFile.DateOfFlight == default
                || String.IsNullOrEmpty(parsedFile.GliderId)
            )
                throw new Exception("Invalid IGC File");

            if (parsedFile.Waypoints.Count >= 2
                && parsedFile.Waypoints.First().Time.Date != parsedFile.Waypoints.Last().Time.Date
            )
                SubtractOneDay(parsedFile);

            return parsedFile;
        }

        /// <summary>
        /// Because the flight date is the date the aircraft LANDED, subtract 1 day from all dates to change it
        /// to the date the aircraft took off.
        /// </summary>
        /// <param name="parsedFile"></param>
        private static void SubtractOneDay(IgcFile parsedFile)
        {
            parsedFile.DateOfFlight = parsedFile.DateOfFlight.AddDays(-1);

            foreach (var waypoint in parsedFile.Waypoints)
                waypoint.Time = waypoint.Time.AddDays(-1);
        }

        private static Waypoint ParseWaypoint(DateTime previousTimestamp, string line)
        {
            // B hhmmss #######N ########W A PPPPP GGGGG AAA CC EEE
            string time = line.Substring(1, 6);

            // UTC
            int hour = Int32.Parse(time.Substring(0, 2));
            int minutes = Int32.Parse(time.Substring(2, 2));
            int seconds = Int32.Parse(time.Substring(4, 2));

            string latitude = line.Substring(7, 8);
            string longitude = line.Substring(15, 9);

            string pressureAltitude = line.Substring(25, 5);
            string gpsAltitude = line.Substring(30, 5);

            //string gpsAccuracy = line.Substring(34, 3);
            //string numSatelites = line.Substring(37, 2);
            //string engineNoise = line.Substring(29, 3);

            var waypoint = new Waypoint()
            {
                GpsAltitude = Int32.Parse(gpsAltitude),
                Latitude = ParseCoordinate(latitude),
                Longitude = ParseCoordinate(longitude),
                Time = ParseTimestamp(previousTimestamp, hour, minutes, seconds)
            };

            return waypoint;
        }

        private static DateTime ParseTimestamp(DateTime previousTimestamp, int hour, int minutes, int seconds)
        {
            var date = previousTimestamp.Date.Add(new TimeSpan(hour, minutes, seconds));

            // Add a day to account for if the time moves from 11:59 to 00:00
            // https://github.com/skyhop/Igc/blob/master/Skyhop.Igc/Parser.cs#L514
            if (date < previousTimestamp.AddHours(-1))
                date = date.AddDays(1);

            return date;
        }

        private static double ParseCoordinate(string coordinate)
        {
            int degrees;
            double minutes;
            int multiplier = 1;

            if (coordinate.Length == 9)
            {
                // E/W
                degrees = Int32.Parse(coordinate.Substring(0, 3));
                minutes = Double.Parse(coordinate.Substring(3, 2) + "." + coordinate.Substring(5, 3));
                if (coordinate[8] == 'W')
                    multiplier = -1;
            }
            else
            {
                // N/S
                degrees = Int32.Parse(coordinate.Substring(0, 2));
                minutes = Double.Parse(coordinate.Substring(2, 2) + "." + coordinate.Substring(4, 3));
                if (coordinate[7] == 'S')
                    multiplier = -1;
            }

            return Math.Round((degrees + minutes / 60), 4) * multiplier;
        }
    }
}
