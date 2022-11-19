using Dapper;
using GliderView.Data.Models;
using GliderView.Service;
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
    public class FlightRepository : IFlightRepository
    {
        private readonly string _connectionString;

        public FlightRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private SqlConnection GetOpenConnection()
        {
            var con = new SqlConnection(_connectionString);
            if (con.State != ConnectionState.Open)
            {
                con.Open();
            }
            return con;
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
    , A.IsGlider
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
    , TowAircraft.AircraftGuid AS TowAircraftId
    , TowAircraft.Description AS TowAircraftDescription
    , TowAircraft.Registration AS TowAircraftRegistration
    , TowAircraft.TrackerId AS TowAircraftTrackerId

    , A.AircraftGuid AS AircraftId
    , A.TrackerId
    , A.Description AS AircraftDescription
    , A.Registration AS AircraftRegistration
    , A.NumSeats
    , A.IsGlider
FROM dbo.Flight F
    LEFT JOIN dbo.Aircraft A
        ON F.AircraftId = A.AircraftId
    LEFT JOIN dbo.Flight Tow
        ON F.TowId = Tow.FlightId
    LEFT JOIN dbo.Aircraft TowAircraft
        ON Tow.AircraftId = TowAircraft.AircraftId
WHERE F.StartDate >= @startDate
    AND F.StartDate <= @endDate
    AND (@aircraftId IS NULL OR A.AircraftGuid = @aircraftId)
    AND (@pilotId IS NULL OR EXISTS (
        SELECT
            1
        FROM dbo.Occupant O
            JOIN dbo.[User] U
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
                    .Select(flight => ConvertDataToService(flight)!)
                    .ToList();
            }
        }

        public async Task AssignTow(Guid gliderFlightId, Guid towPlaneFlightId)
        {
            const string sql = @"
UPDATE dbo.Flight
    SET TowId = (
        SELECT
            FlightId
        FROM Flight
        WHERE FlightGuid = @towPlaneFlightId
            AND IsDeleted = 0
    )
WHERE FlightGuid = @gliderFlightId
";
            using (var con = new SqlConnection(_connectionString))
            {
                var args = new
                {
                    gliderFlightId,
                    towPlaneFlightId
                };
                await con.ExecuteAsync(sql, args);
            }
        }

        public async Task AddFlight(Service.Models.Flight flight)
        {
            using (var con = GetOpenConnection())
            using (IDbTransaction tran = await con.BeginTransactionAsync())
            {
                // Insert Flight
                Guid flightId = await InsertFlight(flight, tran);
                flight.FlightId = flightId;

                // Insert waypoints
                if (flight.Waypoints != null && flight.Waypoints.Any())
                    await InsertWaypoints(flight, tran);

                tran.Commit();
            }
        }

        private Task<Guid> InsertFlight(Service.Models.Flight flight, IDbTransaction tran)
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
                TowFlightId = flight.TowFlight?.FlightId,
                flight.IgcFileName
            };
            return tran.Connection.ExecuteScalarAsync<Guid>(sql, flightArgs, tran);

        }

        private Task InsertWaypoints(Service.Models.Flight flight, IDbTransaction tran)
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
            return tran.Connection.ExecuteAsync(sql, args, tran);
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
                        TrackerId = flight.TrackerId,
                        IsGlider = flight.IsGlider
                    },
                TowFlight = flight.TowFlightId == null
                    ? null
                    : new Service.Models.Flight()
                    {
                        FlightId = flight.TowFlightId.Value,
                        Aircraft = flight.TowAircraftId == null
                            ? null
                            : new Aircraft()
                            {
                                AircraftId = flight.TowAircraftId.Value,
                                Description = flight.TowFlightDescription,
                                RegistrationId = flight.TowFlightRegistration,
                                TrackerId = flight.TowFlightTrackerId
                            }
                    }
            };
        }

        public async Task<Aircraft?> GetAircraftByTrackerId(string trackerId)
        {
            const string sql = @"
SELECT
    A.AircraftGuid AS AircraftId
    , A.Description
    , A.TrackerId
    , A.Registration AS RegistrationId
    , A.NumSeats
    , A.IsGlider
FROM dbo.Aircraft A
WHERE A.TrackerId = @trackerId
    AND A.IsDeleted = 0;
";
            using (var con = new SqlConnection(_connectionString))
            {
                var args = new
                {
                    trackerId
                };
                return await con.QueryFirstOrDefaultAsync<Aircraft?>(sql, args);
            }
        }

        public async Task AddAircraft(Aircraft aircraft)
        {
            const string sql = @"
DECLARE @id UNIQUEIDENTIFIER = NEWID();

INSERT INTO dbo.Aircraft (
      AircraftGuid
    , Description
    , Registration
    , TrackerId
    , NumSeats
    , IsGlider
)
VALUES (
      @id
    , @description
    , @registration
    , @trackerId
    , @numSeats
    , @isGlider
)

SELECT @id;
";

            using (var con = new SqlConnection(_connectionString))
            {
                var args = new
                {
                    aircraft.Description,
                    aircraft.NumSeats,
                    Registration = aircraft.RegistrationId,
                    aircraft.TrackerId,
                    aircraft.IsGlider
                };
                Guid id = await con.ExecuteScalarAsync<Guid>(sql, args);
                aircraft.AircraftId = id;
            }
        }
    }
}
