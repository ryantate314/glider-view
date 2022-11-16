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
            public decimal Latitude { get; set; }
            public decimal Longitude { get; set; }
        }

        public string GliderType { get; set; }
        public string GliderId { get; set; }

        public DateTime DateOfFlight { get; set; }

        public List<Waypoint> Waypoints { get; set; } = new List<Waypoint>();

        public static IgcFile Parse(Stream fileStream)
        {
            const string GLIDER_TYPE = "HFGTYGLIDERTYPE";
            const string GLIDER_ID = "HFGIDGLIDERID";
            const string DATE = "HFDTE";

            var parsedFile = new IgcFile();

            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true))
            {
                string? line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (line.StartsWith("B"))
                    {
                        parsedFile.Waypoints.Add(
                            ParseWaypoint(parsedFile.DateOfFlight, line)
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

                        parsedFile.DateOfFlight = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
                    }
                }
            }

            if (parsedFile.DateOfFlight == default
                || String.IsNullOrEmpty(parsedFile.GliderId)
            )
            {
                throw new Exception("Invalid IGC File");
            }

            return parsedFile;
        }

        private static Waypoint ParseWaypoint(DateTime date, string line)
        {
            // B hhmmss #######N ########W A PPPPP GGGGG AAA CC EEE
            string time = line.Substring(1, 6);
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
                Time = date + new TimeSpan(hour, minutes, seconds)
            };

            return waypoint;
        }

        private static decimal ParseCoordinate(string coordinate)
        {
            int degrees;
            decimal minutes;
            int multiplier = 1;

            if (coordinate.Length == 9)
            {
                // E/W
                degrees = Int32.Parse(coordinate.Substring(0, 3));
                minutes = Decimal.Parse(coordinate.Substring(3, 2) + "." + coordinate.Substring(5, 3));
                if (coordinate[8] == 'W')
                    multiplier = -1;
            }
            else
            {
                // N/S
                degrees = Int32.Parse(coordinate.Substring(0, 2));
                minutes = Decimal.Parse(coordinate.Substring(2, 2) + "." + coordinate.Substring(4, 3));
                if (coordinate[7] == 'S')
                    multiplier = -1;
            }

            return Math.Round((degrees + minutes / 60), 4) * multiplier;
        }
    }
}
