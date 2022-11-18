using Dapper;
using GliderView.Service.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dapper.SqlMapper;

namespace GliderView.Data
{
    public static class DataExtensions
    {
        public static ICustomQueryParameter AsTableValuedParameter(this IEnumerable<Waypoint> waypoints)
        {
            var table = new DataTable();
            table.Columns.Add("Latitude", typeof(decimal));
            table.Columns.Add("Longitude", typeof(decimal));
            table.Columns.Add("GpsAltitudeMeters", typeof(Int32));
            table.Columns.Add("Date", typeof(DateTime));

            foreach (var waypoint in waypoints)
            {
                table.Rows.Add(
                    waypoint.Latitude,
                    waypoint.Longitude,
                    waypoint.GpsAltitude,
                    waypoint.Time
                );
            }

            return table.AsTableValuedParameter("Waypoint");
        }
    }
}
