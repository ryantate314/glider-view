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
    public class FlightRepository : SqlRepository, IFlightRepository
    {

        public FlightRepository(string connectionString)
            : base(connectionString)
        {
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

    -- Flight Statistics
    , FS.MaxAltitude
    , FS.ReleaseHeight
    , FS.AltitudeGained
    , FS.DistanceTraveled
    , FS.PatternEntryAltitude
FROM dbo.Flight F
    LEFT JOIN dbo.Aircraft A
        ON F.AircraftId = A.AircraftId
    LEFT JOIN dbo.Flight Tow
        ON F.TowId = Tow.FlightId
    LEFT JOIN dbo.Aircraft TowAircraft
        ON Tow.AircraftId = TowAircraft.AircraftId
    LEFT JOIN dbo.FlightStatistics FS
        ON F.FlightId = FS.FlightId
            AND FS.IsDeleted = 0
WHERE F.FlightGuid = @flightId
    AND F.IsDeleted = 0
";
            using (var con = GetOpenConnection())
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

    -- Flight Statistics
    , FS.MaxAltitude
    , FS.ReleaseHeight
    , FS.AltitudeGained
    , FS.DistanceTraveled
    , FS.PatternEntryAltitude
FROM dbo.Flight F
    LEFT JOIN dbo.Aircraft A
        ON F.AircraftId = A.AircraftId
    LEFT JOIN dbo.Flight Tow
        ON F.TowId = Tow.FlightId
    LEFT JOIN dbo.Aircraft TowAircraft
        ON Tow.AircraftId = TowAircraft.AircraftId
    LEFT JOIN dbo.FlightStatistics FS
        ON F.FlightId = FS.FlightId
            AND FS.IsDeleted = 0
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
            using (var con = GetOpenConnection())
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

        public async Task<List<Waypoint>> GetWaypoints(Guid flightId)
        {
            const string sql = @"
SELECT
      W.WaypointId
    , W.[Date] AS [Time]
    , W.Latitude
    , W.Longitude
    , W.GpsAltitudeMeters AS GpsAltitude
    , W.FlightEvent
FROM Flight F
    JOIN Waypoint W
        ON F.FlightId = W.FlightId
WHERE F.FlightGuid = @flightId
    AND F.IsDeleted = 0;
";
            using (var con = GetOpenConnection())
            {
                return (await con.QueryAsync<Waypoint>(sql, new { flightId }))
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
            using (var con = GetOpenConnection())
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
    , FlightEvent
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
    , W.FlightEvent
FROM @waypoints W
";
            var args = new
            {
                flight.FlightId,
                waypoints = flight.Waypoints!.AsTableValuedParameter()
            };
            return tran.Connection.ExecuteAsync(sql, args, tran);
        }

        public async Task UpsertFlightStatistics(Service.Models.Flight flight)
        {
            const string sql = @"
DECLARE @flightId INT = (
    SELECT F.FlightId
    FROM Flight F
    WHERE F.FlightGuid = @flightGuid
        AND F.IsDeleted = 0
);

IF @flightId IS NULL
    THROW 51000, 'Flight does not exist.', 1;

UPDATE FlightStatistics
    SET IsDeleted = 1
WHERE FlightId = @flightId
    AND IsDeleted = 0;

INSERT INTO FlightStatistics (
      FlightId
    , ReleaseHeight
	, AltitudeGained
	, DistanceTraveled
    , MaxAltitude
    , PatternEntryAltitude
)
VALUES (
      @flightId
    , @releaseHeight
    , @altitudeGained
    , @distanceTraveled
    , @maxAltitude
    , @patternEntryAltitude
)
";
            var args = new
            {
                FlightGuid = flight.FlightId,
                ReleaseHeight = flight.Statistics?.ReleaseHeight,
                MaxAltitude = flight.Statistics?.MaxAltitude,
                AltitudeGained = flight.Statistics?.AltitudeGained,
                DistanceTraveled = flight.Statistics?.DistanceTraveled,
                PatternEntryAltitude = flight.Statistics?.PatternEntryAltitude
            };
            using (var con = GetOpenConnection())
                await con.ExecuteAsync(sql, args);
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
                                Description = flight.TowAircraftDescription,
                                RegistrationId = flight.TowAircraftRegistration,
                                TrackerId = flight.TowAircraftTrackerId
                            }
                    },
                Statistics = new FlightStatistics()
                {
                    AltitudeGained = flight.AltitudeGained,
                    DistanceTraveled = flight.DistanceTraveled,
                    MaxAltitude = flight.MaxAltitude,
                    ReleaseHeight = flight.ReleaseHeight,
                    PatternEntryAltitude = flight.PatternEntryAltitude
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
            using (var con = GetOpenConnection())
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

            using (var con = GetOpenConnection())
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

        public async Task UpdateFlightEvents(Service.Models.Flight flight)
        {
            if (flight.Waypoints == null)
                return;

            const string sql = @"
BEGIN TRAN

BEGIN TRY
    UPDATE Waypoint
        SET FlightEvent = NULL
    WHERE FlightId = (
        SELECT
            FlightId
        FROM Flight
        WHERE FlightGuid = @flightId
            AND IsDeleted = 0
    );

    UPDATE W
        SET FlightEvent = NW.FlightEvent
    FROM Waypoint W
        JOIN @waypoints NW
            ON W.WaypointId = NW.WaypointId;

    COMMIT TRAN

END TRY
BEGIN CATCH
    ROLLBACK TRAN;
    THROW;
END CATCH
";
            var args = new
            {
                waypoints = flight.Waypoints.Where(x => x.FlightEvent != null)
                    .AsTableValuedParameter(),
                flight.FlightId
            };
            using (var con = GetOpenConnection())
            {
                await con.ExecuteAsync(sql, args);
            }
        }
    }
}
