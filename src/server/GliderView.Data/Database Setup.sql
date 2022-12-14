USE master
GO

CREATE LOGIN gliderViewer WITH PASSWORD = '************';

USE GliderView
GO

CREATE USER gliderViewer FOR LOGIN gliderViewer;

--ALTER USER gliderViewer WITH LOGIN = gliderViewer;

GRANT SELECT, INSERT, UPDATE, EXECUTE ON schema::dbo TO gliderViewer;
GO

CREATE SCHEMA HangFire;

GO

ALTER AUTHORIZATION ON SCHEMA::HangFire TO gliderViewer;
GRANT INSERT, UPDATE, DELETE ON SCHEMA::HangFire TO gliderViewer;
GRANT CREATE TABLE TO gliderViewer;

GO

CREATE TABLE Aircraft (
	AircraftId INT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	AircraftGuid UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
	TrackerId CHAR(6),
	Description VARCHAR(32),
	Registration CHAR(6),
	NumSeats TINYINT,
	IsGlider BIT,
	AddedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	IsDeleted BIT NOT NULL DEFAULT 0
)

CREATE TABLE Flight (
	FlightId INT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	FlightGuid UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
	StartDate DATETIME NOT NULL,
	EndDate DATETIME,
	AircraftId INT FOREIGN KEY REFERENCES Aircraft (AircraftId),
	TowId INT FOREIGN KEY REFERENCES Flight (FlightId),
	IgcFilename VARCHAR(255),
	AddedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	IsDeleted BIT NOT NULL DEFAULT 0
)

-- One flight has many waypoints
CREATE TABLE Waypoint (
	WaypointId INT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	FlightId INT NOT NULL FOREIGN KEY REFERENCES Flight (FlightId),
	Latitude DECIMAL(6,4) NOT NULL,
	Longitude DECIMAL(7,4) NOT NULL,
	GpsAltitudeMeters SMALLINT,
	[Date] DATETIME NOT NULL,
	FlightEvent TINYINT FOREIGN KEY REFERENCES FlightEventType (FlightEventTypeId)
)



CREATE TABLE UserRole (
	RoleId INT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	RoleCode CHAR(1) NOT NULL UNIQUE,
	Description VARCHAR(100) NOT NULL
)

INSERT INTO UserRole (
	RoleCode
	, Description
)
VALUES (
	'A'
	, 'Admin'
), (
	'U'
	, 'User'
);

CREATE TABLE [User] (
	UserId INT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	UserGuid UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
	IdentityId VARCHAR(100),
	[Role] CHAR(1) NOT NULL FOREIGN KEY REFERENCES UserRole (RoleCode),
	Email VARCHAR(255) NOT NULL,
	Name VARCHAR(255) NOT NULL,
	HashedPassword VARCHAR(255),
	FailedLoginAttempts TINYINT NOT NULL DEFAULT 0,
	IsLocked BIT NOT NULL DEFAULT 0,
	AddedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	IsDeleted BIT NOT NULL DEFAULT 0
)

INSERT INTO [User] (
	Email
	, Name
	, Role
	, HashedPassword
)
VALUES (
	'ryantate314@gmail.com'
	, 'Ryan T'
	, 'A'
	, 'AQAAAAIAAYagAAAAEBusr7qrvh8x4L8HJrYBQ941l4s4rRH2BWyCiRnaagvqM2f6nfvo5unZbtlS1P55Hw=='
)

CREATE TABLE Invitation (
	InvitationId INT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	UserId INT NOT NULL FOREIGN KEY REFERENCES [User] (UserId),
	Token CHAR(16) NOT NULL,
	IssuedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	ExpirationDate DATETIME NOT NULL,
	IsDeleted BIT NOT NULL DEFAULT 0
)

CREATE TABLE Occupant (
	OccupantId INT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	FlightId INT NOT NULL FOREIGN KEY REFERENCES Flight (FlightId),
	UserId INT FOREIGN KEY REFERENCES [User] (UserId),
	FlightNumber INT,
	Notes VARCHAR(MAX),
	Name VARCHAR(100)
)

CREATE TABLE FlightStatistics (
	FlightStatisticsId INT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	FlightId INT NOT NULL FOREIGN KEY REFERENCES Flight (FLightId),
	ReleaseHeight INT,
	AltitudeGained INT,
	DistanceTraveled FLOAT,
	MaxAltitude INT,
	PatternEntryAltitude INT,
	DateAdded DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	IsDeleted BIT NOT NULL DEFAULT 0
);

CREATE TABLE FlightEventType (
	FlightEventTypeId TINYINT NOT NULL PRIMARY KEY,
	[Description] VARCHAR(32)
)

INSERT INTO FlightEventType (
	FlightEventTypeId
	, [Description]
)
VALUES
  ( 1, 'Release' )
, ( 2, 'Pattern Entry' )

GO

CREATE TYPE Waypoint AS TABLE (
	WaypointId INT,
	Latitude DECIMAL(6,4) NOT NULL,
	Longitude DECIMAL(7,4) NOT NULL,
	GpsAltitudeMeters SMALLINT,
	[Date] DATETIME NOT NULL,
	FlightEvent TINYINT
);

CREATE TYPE IdList AS TABLE (
	Id UNIQUEIDENTIFIER NOT NULL
);

GO


/* ROLLBACK */

/*

USE GliderView

GO

DROP TABLE Occupant
DROP TABLE Invitation
DROP TABLE [User]
DROP TABLE UserRole
--DROP TABLE Waypoint
--DROP TABLE FlightStatistics
--DROP TABLE Flight
DROP TABLE Aircraft
DROP TABLE FlightEventType

DROP TYPE Waypoint

DROP USER gliderViewer

GO

*/