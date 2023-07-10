USE GliderView
GO

CREATE TABLE Rates (
	RateId INT PRIMARY KEY IDENTITY(1, 1),
	EffectiveDate DATETIME NOT NULL,
	ExpirationDate DATETIME NULL,

	HookupCost SMALLMONEY NOT NULL,
	CostPerHundredFeet SMALLMONEY NOT NULL,
	MinTowHeight INT NOT NULL
);

GO

CREATE TABLE AircraftRates (
	AircraftRateId INT PRIMARY KEY IDENTITY(1, 1),
	AircraftId INT NOT NULL,

	EffectiveDate DATETIME NOT NULL,
	ExpirationDate DATETIME NULL,

	RentalCostPerHour SMALLMONEY NOT NULL,
	MinRentalHours FLOAT NOT NULL,
	CONSTRAINT FK_AircraftRates_AircraftId_Aircraft
		FOREIGN KEY (AircraftId) REFERENCES Aircraft (AircraftId)
);

GO

CREATE TABLE Airfield (
	AirfieldId INT PRIMARY KEY,
	FaaId VARCHAR(5) NOT NULL
		CONSTRAINT UQ_Airfield_FaaId
		UNIQUE,
	ElevationMeters INT NOT NULL
)

GO

ALTER TABLE dbo.Flight
	ADD AirfieldId INT
		CONSTRAINT FK_Flight_AirfieldId_Airfield
		FOREIGN KEY REFERENCES Airfield (AirfieldId);

GO


INSERT INTO Rates (
	EffectiveDate
	, HookupCost
	, CostPerHundredFeet
	, MinTowHeight
)
VALUES (
	'2023-01-01',
	22.50,
	1.75,
	1000
)

GO

INSERT INTO AircraftRates (
	AircraftId
	, EffectiveDate
	, RentalCostPerHour
	, MinRentalHours
)
SELECT
	AircraftId
	, '2023-01-01'
	, CASE A.Registration
		WHEN 'N2750H'
			THEN 48
		WHEN 'N327PW'
			THEN 72
		WHEN 'N410PW'
			THEN 48
		WHEN 'N17934'
			THEN 40
		END
	, 0.25
FROM dbo.Aircraft A
WHERE Registration IN (
	'N2750H'
	,'N327PW'
	,'N410PW'
	,'N17934'
)

GO

INSERT INTO dbo.Airfield (
	AirfieldId
	, FaaId
	, ElevationMeters
)
VALUES (
	1
	, '92A'
	, 235
)

UPDATE dbo.Flight
	SET AirfieldId = 1;

GO


/* ROLLBACK */

/*

DROP TABLE dbo.AircraftRates
DROP TABLE dbo.Rates
GO

*/