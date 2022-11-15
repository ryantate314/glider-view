using Dapper;
using GliderView.Data.Models;
using GliderView.Service.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Data
{
    public class FlightRepository
    {
        private readonly string _connectionString;

        public FlightRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Service.Models.Flight?> GetFlight(Guid flightId)
        {
            const string sql = @"
SELECT
    F.FlightGuid AS FlightId
    , F.StartDate
    , F.EndDate
    , F.IgcFilename
    -- The tow plane flight
    , Tow.FlightGuid AS TowFlightId

    , A.AircraftGuid AS AircraftId
    , A.TrackerId
    , A.Description AS AircraftDescription
    , A.Registration AS AircraftRegistration
    , A.NumSeats
FROM dbo.Flight F
    LEFT JOIN dbo.Aircraft A
        ON F.AircraftId = A.AircraftId
    LEFT JOIN dbo.Flight Tow
        ON F.TowId = Tow.FlightId
WHERE F.FlightGuid = @flightId
    AND F.IsDeleted = 0
";
            using (var con = new SqlConnection(_connectionString))
            {
                var args = new
                {
                    flightId
                };
                var flight = await con.QueryFirstOrDefaultAsync<Data.Models.Flight>(sql, args);
                return ConvertDataToService(flight);
            }
        }

        public async Task<List<Service.Models.Flight>> GetFlights(FlightSearch search)
        {
            const string sql = @"
SELECT
    F.FlightGuid AS FlightId
    , F.StartDate
    , F.EndDate
    , F.IgcFilename
    -- The tow plane flight
    , Tow.FlightGuid AS TowFlightId

    , A.AircraftGuid AS AircraftId
    , A.TrackerId
    , A.Description AS AircraftDescription
    , A.Registration AS AircraftRegistration
    , A.NumSeats
FROM dbo.Flight F
    LEFT JOIN dbo.Aircraft A
        ON F.AircraftId = A.AircraftId
    LEFT JOIN dbo.Flight Tow
        ON F.TowId = Tow.FlightId
WHERE F.StartDate >= @startDate
    AND F.StartDate <= @endDate
    AND (@aircraftId IS NULL OR A.AircraftGuid = @aircraftId)
    AND (@pilotId IS NULL OR EXISTS (
        SELECT
            1
        FROM dbo.Occupant O
            JOIN dbo.User U
                ON O.UserId = U.UserId
        WHERE U.UserGuid = @pilotId
            AND F.FlightId = O.FlightId
    ))
    AND F.IsDeleted = 0
";
            using (var con = new SqlConnection(_connectionString))
            {
                var args = new
                {
                    StartDate = search.StartDate!,
                    EndDate = search.EndDate!,
                    PilotId = search.PilotId,
                    AircraftId = search.AircraftId
                };
                return (await con.QueryAsync<Data.Models.Flight>(sql, args))
                    .Select(ConvertDataToService)
                    .ToList();
            }
        }

        public async Task AddFlight(Service.Models.Flight flight)
        {
            using (var con = new SqlConnection(_connectionString))
            using (var tran = await con.BeginTransactionAsync())
            {
                // Insert Flight
                Guid flightId = await InsertFlight(flight, con);
                flight.FlightId = flightId;

                // Insert waypoints
                if (flight.Waypoints != null && flight.Waypoints.Any())
                    await InsertWaypoints(flight, con);

                tran.Commit();
            }
        }

        private Task<Guid> InsertFlight(Service.Models.Flight flight, IDbConnection con)
        {
            const string sql = @"
INSERT INTO dbo.Flight (
    StartDate
    , EndDate
    , AircraftId
    , TowId
    , IgcFilename
)
VALUES (
    @startDate
    , @endDate
    , (
        SELECT
            AircraftId
        FROM dbo.Aircraft A
        WHERE A.AircraftGuid = @aircraftId
            AND A.IsDeleted = 0
    )
    , (
        SELECT
            FlightId
        FROM dbo.Flight F
        WHERE F.FlightGuid = @towFlightId
            AND F.IsDeleted = 0
    )
    , @igcFileName
);

SELECT
    FlightGuid
FROM dbo.Flight F
WHERE F.FlightId = SCOPE_IDENTITY();
";
            var flightArgs = new
            {
                flight.StartDate,
                flight.EndDate,
                flight.Aircraft?.AircraftId,
                flight.TowFlightId,
                flight.IgcFileName
            };
            return con.ExecuteScalarAsync<Guid>(sql, flightArgs);
            
        }

        private Task InsertWaypoints(Service.Models.Flight flight, IDbConnection con)
        {
            const string sql = @"
INSERT INTO dbo.Waypoint (
    FlightId
    , Latitude
    , Longitude
    , GpsAltitudeMeters
    , [Date]
)
SELECT
    (
        SELECT
            FlightId
        FROM dbo.Flight F
        WHERE F.FlightGuid = @flightId
    )
    , W.Latitude
    , W.Longitude
    , W.GpsAltitudeMeters
    , W.[Date]
FROM @waypoints W
";
            var args = new
            {
                flight.FlightId,
                waypoints = flight.Waypoints!.AsTableValuedParameter()
            };
            return con.ExecuteAsync(sql, args);
        }

        private Service.Models.Flight? ConvertDataToService(Data.Models.Flight flight)
        {
            if (flight == null)
                return null;

            return new Service.Models.Flight()
            {
                EndDate = flight.EndDate,
                FlightId = flight.FlightId,
                IgcFileName = flight.IgcFilename,
                StartDate = flight.StartDate,
                Aircraft = flight.AircraftId == null
                    ? null
                    : new Aircraft()
                    {
                        AircraftId = flight.AircraftId.Value,
                        Description = flight.AircraftDescription!,
                        NumSeats = flight.NumSeats,
                        RegistrationId = flight.AircraftRegistration,
                        TrackerId = flight.TrackerId
                    }
            };
        }
    }
}
