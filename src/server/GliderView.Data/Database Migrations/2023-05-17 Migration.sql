USE GliderView
GO

ALTER TABLE Invitation
	ALTER COLUMN Token CHAR(32) NOT NULL

ALTER TABLE [User]
	ADD ModifiedDate DATETIME NULL;

CREATE TABLE dbo.Statistic (
	StatisticId INT PRIMARY KEY,
	[Description] VARCHAR(32) NOT NULL
);

DECLARE @releaseHeight INT = 1;
DECLARE @altitudeGained INT = 2;
DECLARE @distanceTraveled INT = 3;
DECLARE @maxAltitude INT = 4;
DECLARE @patternAltitude INT = 5;

INSERT INTO dbo.Statistic VALUES
  (@releaseHeight, 'Release Height')
, (@altitudeGained, 'Altitude Gained')
, (@distanceTraveled, 'Distance Traveled')
, (@maxAltitude, 'Max Altitude')
, (@patternAltitude, 'Pattern Altitude');

CREATE TABLE dbo.FlightStatisticsNew (
	FLightStatisticId INT PRIMARY KEY IDENTITY(1, 1),
	FlightId INT NOT NULL,
	StatisticId INT NOT NULL,
	[Value] FLOAT NULL,
	DateAdded DATETIME NOT NULL
		CONSTRAINT DF_FlightStatistics_DateAdded
		DEFAULT CURRENT_TIMESTAMP,
	IsDeleted BIT NOT NULL
		CONSTRAINT DF_FlightStatistics_IsDeleted
		DEFAULT 0,
	CONSTRAINT FK_FlightStatistics_FlightId_Flight
		FOREIGN KEY (FlightId) REFERENCES dbo.Flight (FlightId),
	CONSTRAINT FK_FlightStatistics_StatisticId_Statistic
		FOREIGN KEY (StatisticId) REFERENCES dbo.Statistic (StatisticId)
);

GO

DECLARE @releaseHeight INT = 1;
DECLARE @altitudeGained INT = 2;
DECLARE @distanceTraveled INT = 3;
DECLARE @maxAltitude INT = 4;
DECLARE @patternAltitude INT = 5;

INSERT INTO dbo.FlightStatisticsNew (
	FlightId,
	StatisticId,
	[Value],
	DateAdded,
	IsDeleted
)
SELECT
	A.FlightId
	, A.StatisticId
	, A.[Value]
	, A.DateAdded
	, A.IsDeleted
FROM (
	SELECT
		FS.FLightId
		, @releaseHeight AS StatisticId
		, FS.ReleaseHeight AS [Value]
		, FS.DateAdded
		, FS.IsDeleted
	FROM dbo.FlightStatistics FS

	UNION ALL

	SELECT
		FS.FLightId
		, @altitudeGained AS StatisticId
		, FS.AltitudeGained AS [Value]
		, FS.DateAdded
		, FS.IsDeleted
	FROM dbo.FlightStatistics FS

	UNION ALL

	SELECT
		FS.FLightId
		, @distanceTraveled AS StatisticId
		, FS.DistanceTraveled AS [Value]
		, FS.DateAdded
		, FS.IsDeleted
	FROM dbo.FlightStatistics FS

	UNION ALL

	SELECT
		FS.FLightId
		, @maxAltitude AS StatisticId
		, FS.MaxAltitude AS [Value]
		, FS.DateAdded
		, FS.IsDeleted
	FROM dbo.FlightStatistics FS

	UNION ALL

	SELECT
		FS.FLightId
		, @patternAltitude AS StatisticId
		, FS.PatternEntryAltitude AS [Value]
		, FS.DateAdded
		, FS.IsDeleted
	FROM dbo.FlightStatistics FS
) A
ORDER BY A.DateAdded, A.FlightId, A.StatisticId

--TRUNCATE TABLE dbo.FlightStatisticsNew


EXEC sp_rename 'dbo.FlightStatistics', 'FlightStatisticsOld';

EXEC sp_rename 'dbo.FlightStatisticsNew', 'FlightStatistics';

DROP TABLE dbo.FlightStatisticsOld;

CREATE TYPE dbo.Statistic AS TABLE (
	StatisticId INT NOT NULL
	, [Value] FLOAT NULL
);