using Dapper;
using GliderView.Data.Models;
using GliderView.Service;
using GliderView.Service.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dapper.SqlMapper;

namespace GliderView.Data
{
    public class FlightRepository : SqlRepository, IFlightRepository
    {

        private const string FLIGHT_SELECT = @"
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
";
        private readonly ILogger<FlightRepository> _log;

        public FlightRepository(string connectionString, ILogger<FlightRepository> log)
            : base(connectionString)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }


        public async Task<Service.Models.Flight?> GetFlight(Guid flightId)
        {
            const string sql = FLIGHT_SELECT + @"
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
            const string sql = FLIGHT_SELECT + @"
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

        private async Task<List<Service.Models.Flight>> GetFlights(IEnumerable<Guid> flightIds, IDbConnection connection)
        {
            const string sql = FLIGHT_SELECT + @"
WHERE F.FlightGuid IN (
    SELECT Id
    FROM @flightIds
)
    AND F.IsDeleted = 0
";
            var args = new
            {
                flightIds = flightIds.AsTableValuedParameter()
            };
            var flights = await connection.QueryAsync<Data.Models.Flight>(sql, args);
            return flights.Select(flight => ConvertDataToService(flight)!)
                .ToList();
        }

        public async Task<FlightStatistics> GetStatistics(Guid flightId)
        {
            Dictionary<Guid, FlightStatistics> stats = await GetStatistics(new Guid[] { flightId });
            if (stats.ContainsKey(flightId))
                return stats[flightId];
            else
                return new FlightStatistics();
        }

        public async Task<Dictionary<Guid, FlightStatistics>> GetStatistics(IEnumerable<Guid> flightIds)
        {
            const string sql = @"
SELECT
    F.FlightGuid AS FlightId
    , FS.StatisticId AS Statistic
    , FS.[Value]
FROM dbo.Flight F
    JOIN dbo.FlightStatistics FS
        ON F.FlightId = FS.FlightId
WHERE F.IsDeleted = 0
    AND FS.IsDeleted = 0
    AND F.FlightGuid IN (
        SELECT ID FROM @flightIds
    )
";
            using (var con = GetOpenConnection())
            {
                var stats = await con.QueryAsync<FlightStatistic>(
                    sql,
                    new { flightIds = flightIds.AsTableValuedParameter() }
                );

                return stats.GroupBy(x => x.FlightId)
                    .Select(x => new { FlightId = x.Key, Stats = GetStatisticsFromArray(x) })
                    .ToDictionary(x => x.FlightId, x => x.Stats);
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
    , ModifiedDate = CURRENT_TIMESTAMP
WHERE FlightGuid = @gliderFlightId
    AND IsDeleted = 0;
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
                    await UpsertWaypoints(flight, tran);

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

        private IEnumerable<FlightStatistic> GetStatisticsFromFlight(Service.Models.Flight flight)
        {
            var statistics = new List<FlightStatistic>()
            {
                new FlightStatistic()
                {
                    FlightId = flight.FlightId,
                    Statistic = Statistic.AltitudeGained,
                    Value = flight.Statistics?.AltitudeGained
                },
                new FlightStatistic()
                {
                    FlightId = flight.FlightId,
                    Statistic = Statistic.MaxAltitude,
                    Value = flight.Statistics?.MaxAltitude
                },
                new FlightStatistic()
                {
                    FlightId = flight.FlightId,
                    Statistic = Statistic.PatternAltitude,
                    Value = flight.Statistics?.PatternEntryAltitude
                },
                new FlightStatistic()
                {
                    FlightId = flight.FlightId,
                    Statistic = Statistic.ReleaseHeight,
                    Value = flight.Statistics?.ReleaseHeight
                },
                new FlightStatistic()
                {
                    FlightId = flight.FlightId,
                    Statistic = Statistic.DistanceTraveled,
                    Value = flight.Statistics?.DistanceTraveled
                },
                new FlightStatistic()
                {
                    FlightId = flight.FlightId,
                    Statistic = Statistic.MaxDistanceFromField,
                    Value = flight.Statistics?.MaxDistanceFromField
                }
            };
            return statistics;
        }

        private FlightStatistics GetStatisticsFromArray(IEnumerable<FlightStatistic> statistics)
        {
            var stats = new FlightStatistics();
            foreach (var stat in statistics)
            {
                switch (stat.Statistic)
                {
                    case Statistic.AltitudeGained:
                        stats.AltitudeGained = (int?)stat.Value;
                        break;
                    case Statistic.PatternAltitude:
                        stats.PatternEntryAltitude= (int?)stat.Value;
                        break;
                    case Statistic.ReleaseHeight:
                        stats.ReleaseHeight = (int?)stat.Value;
                        break;
                    case Statistic.DistanceTraveled:
                        stats.DistanceTraveled = stat.Value;
                        break;
                    case Statistic.MaxAltitude:
                        stats.MaxAltitude = (int?)stat.Value;
                        break;
                    case Statistic.MaxDistanceFromField:
                        stats.MaxDistanceFromField = stat.Value;
                        break;
                    default:
                       _log.LogWarning("Unknown statistic: " + stat.Statistic);
                        break;
                }
            }
            return stats;
        }

        private Task UpsertWaypoints(Service.Models.Flight flight, IDbTransaction tran)
        {
            const string sql = @"

DECLARE @flightPk INT = (
    SELECT FlightId
    FROM dbo.Flight F
    WHERE F.FlightGuid = @flightId
        AND F.IsDeleted = 0
);

-- TODO: Raise Error

BEGIN TRAN

BEGIN TRY

    DELETE dbo.Waypoint
    WHERE FlightId = @flightPk;

    INSERT INTO dbo.Waypoint (
        FlightId
        , Latitude
        , Longitude
        , GpsAltitudeMeters
        , [Date]
        , FlightEvent
    )
    SELECT
          @flightPk
        , W.Latitude
        , W.Longitude
        , W.GpsAltitudeMeters
        , W.[Date]
        , W.FlightEvent
    FROM @waypoints W

    COMMIT TRAN
END TRY
BEGIN CATCH
    ROLLBACK TRAN;
    THROW;
END CATCH
";
            var args = new
            {
                flight.FlightId,
                waypoints = flight.Waypoints!.AsTableValuedParameter()
            };
            
            return tran.Connection.ExecuteAsync(sql, args, tran);
        }

        // Don't expost the transaction to the public interface
        public Task UpsertFlightStatistics(Service.Models.Flight flight)
            => UpsertFlightStatistics(flight, null);

        private async Task UpsertFlightStatistics(Service.Models.Flight flight, IDbTransaction? tran)
        {
            IEnumerable<FlightStatistic> statistics = GetStatisticsFromFlight(flight);

            const string sql = @"
DECLARE @flightId INT = (
    SELECT F.FlightId
    FROM Flight F
    WHERE F.FlightGuid = @flightGuid
        AND F.IsDeleted = 0
);

IF @flightId IS NULL
    THROW 51000, 'Flight does not exist.', 1;

BEGIN TRAN
    BEGIN TRY

        UPDATE FlightStatistics
            SET IsDeleted = 1
        WHERE FlightId = @flightId
            AND IsDeleted = 0;

        INSERT INTO FlightStatistics (
              FlightId
            , StatisticId
            , [Value]
        )
        SELECT
              @flightId
            , S.StatisticId
            , S.[Value]
        FROM @statistics S;

        COMMIT TRAN
    END TRY
BEGIN CATCH
    ROLLBACK TRAN;
    THROW;
END CATCH
";
            var args = new
            {
                flightGuid = flight.FlightId,
                statistics = ToTableValueParameter(statistics)
            };
            if (tran == null)
            {
                using (var con = GetOpenConnection())
                    await con.ExecuteAsync(sql, args);
            }
            else
            {
                await tran.Connection.ExecuteAsync(sql, args, tran);
            }   
        }

        private ICustomQueryParameter ToTableValueParameter(IEnumerable<FlightStatistic> statistics)
        {
            var table = new DataTable();
            table.Columns.Add("StatisticId", typeof(int));
            table.Columns.Add("Value", typeof(float));

            foreach (var stat in statistics)
                table.Rows.Add(
                    stat.Statistic,
                    stat.Value
                );

            return table.AsTableValuedParameter("Statistic");
        }

        private Service.Models.Flight? ConvertDataToService(Data.Models.Flight flight)
        {
            if (flight == null)
                return null;

            return new Service.Models.Flight()
            {
                EndDate = DateTime.SpecifyKind(flight.EndDate, DateTimeKind.Utc),
                FlightId = flight.FlightId,
                IgcFileName = flight.IgcFilename,
                StartDate = DateTime.SpecifyKind(flight.StartDate, DateTimeKind.Utc),
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

        public async Task AddPilot(Guid flightId, Guid pilotId)
        {
            const string sql = @"
-- Look up Flight primary key
DECLARE @intFlightId INT = (
    SELECT
        FlightId
    FROM dbo.Flight
    WHERE FlightGuid = @flightId
        AND IsDeleted = 0
);
IF @intFlightId IS NULL
    THROW 51000, 'Flight does not exist.', 1
-- Look up User primary key
DECLARE @intUserId INT = (
    SELECT
        UserId
    FROM dbo.[User]
    WHERE UserGuid = @pilotId
        AND IsDeleted = 0
);
IF @intUserId IS NULL
    THROW 51000, 'User does not exist.', 1

IF NOT EXISTS (SELECT 1 FROM dbo.Occupant WHERE FlightId = @intFlightId AND UserId = @intUserId)
    INSERT INTO dbo.Occupant (
        FlightId
        , UserId
    )
    VALUES (
        @intFlightId
        , @intUserId
    )
";
            using (var con = GetOpenConnection())
            {
                await con.ExecuteAsync(sql, new { flightId, pilotId });
            }
        }

        public async Task<List<LogBookEntry>> GetLogBook(Guid pilotId)
        {
            const string sql = @"
SELECT
    F.FlightGuid AS FlightId
    , O.FlightNumber
    , O.Notes AS Remarks
FROM dbo.Flight F
    JOIN dbo.Occupant O
        ON F.FlightID = O.FlightId
    JOIN dbo.[User] U
        ON O.UserId = U.UserId
WHERE U.UserGuid = @userId
    AND F.IsDeleted = 0
";

            using (var con = GetOpenConnection())
            {
                var args = new
                {
                    userId = pilotId
                };

                List<Service.Models.LogBookEntry> logEntries = (await con.QueryAsync<Service.Models.LogBookEntry>(sql, args))
                    .ToList();

                var flights = await GetFlights(logEntries.Select(x => x.FlightId), con);

                foreach (var flight in flights)
                {
                    logEntries.Find(x => x.FlightId == flight.FlightId)!.Flight = flight;
                }

                return logEntries;
            }

        }

        public async Task RemovePilot(Guid flightId, Guid pilotId)
        {
            const string sql = @"
DELETE O
FROM dbo.Occupant O
    JOIN dbo.[User] U
        ON O.UserId = U.UserId
    JOIN dbo.Flight F
        ON O.FlightId = F.FlightId
WHERE U.UserGuid = @pilotId
    AND F.FlightGuid = @flightId
";
            using (var con = GetOpenConnection())
            {
                await con.ExecuteAsync(sql, new { flightId, pilotId });
            }
        }

        public async Task<IEnumerable<Occupant>> GetPilotsOnFlight(Guid flightId)
        {
            var pilots = await GetPilotsOnFlights(new Guid[] { flightId });
            if (pilots.ContainsKey(flightId))
                return pilots[flightId];
            else
                return new List<Occupant>();
        }

        public async Task<Dictionary<Guid, IEnumerable<Occupant>>> GetPilotsOnFlights(IEnumerable<Guid> flightIds)
        {
            const string sql = @"
SELECT
    F.FlightGuid AS FlightId
    , U.UserGuid AS UserId
    , ISNULL(U.Name, O.Name) AS Name
    , O.Notes
    , O.FlightNumber
FROM dbo.Flight F
    JOIN dbo.Occupant O
        ON F.FlightId = O.FlightId
    LEFT JOIN dbo.[User] U
        ON O.UserId = U.UserId
WHERE F.IsDeleted = 0
    AND F.FlightGuid IN (
        SELECT Id FROM @flightIds
    )
";
            using (var con = GetOpenConnection())
            {
                return (await con.QueryAsync<Service.Models.Occupant>(sql, new { flightIds = flightIds.AsTableValuedParameter() }))
                    .GroupBy(x => x.FlightId)
                    .ToDictionary(x => x.Key, x => x.AsEnumerable());
            }
        }

        public async Task UpdateFlight(Service.Models.Flight flight)
        {
            const string sql = @"
UPDATE dbo.Flight
    SET StartDate = @startDate
    , EndDate = @endDate
    , ModifiedDate = CURRENT_TIMESTAMP
WHERE FlightGuid = @flightId
    AND IsDeleted = 0;
";
            using (var con = await GetOpenConnectionAsync())
            using (var tran = await con.BeginTransactionAsync())
            {
                var args = new
                {
                    FlightId = flight.FlightId,
                    StartDate = flight.StartDate,
                    EndDate = flight.EndDate
                };
                await tran.Connection.ExecuteAsync(sql, args, tran);

                await UpsertWaypoints(flight, tran);

                await UpsertFlightStatistics(flight, tran);

                await tran.CommitAsync();
            }
        }

        public async Task DeleteFlight(Guid flightId)
        {
            const string sql = @"
UPDATE dbo.Flight
    SET IsDeleted = 1
    , ModifiedDate = CURRENT_TIMESTAMP
WHERE FlightGuid = @flightId
    AND IsDeleted = 0;
";
            using (var con = await GetOpenConnectionAsync())
            {
                await con.ExecuteAsync(sql, new { flightId });
            }
        }
    }
}
