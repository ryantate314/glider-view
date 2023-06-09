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
            if (waypoints == null)
                throw new ArgumentNullException(nameof(waypoints));

            var table = new DataTable();
            table.Columns.Add("WaypointId", typeof(int));
            table.Columns.Add("Latitude", typeof(decimal));
            table.Columns.Add("Longitude", typeof(decimal));
            table.Columns.Add("GpsAltitudeMeters", typeof(Int32));
            table.Columns.Add("Date", typeof(DateTime));
            table.Columns.Add("FlightEvent", typeof(int));

            foreach (var waypoint in waypoints)
            {
                table.Rows.Add(
                    waypoint.WaypointId,
                    waypoint.Latitude,
                    waypoint.Longitude,
                    waypoint.GpsAltitude,
                    waypoint.Time,
                    waypoint.FlightEvent
                );
            }

            return table.AsTableValuedParameter("Waypoint");
        }

        public static ICustomQueryParameter AsTableValuedParameter(this IEnumerable<Guid> guids)
        {
            if (guids == null)
                throw new ArgumentNullException(nameof(guids));

            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));

            foreach (var id in guids)
                table.Rows.Add(id);

            return table.AsTableValuedParameter("IdList");
        }
    }
}
