USE GliderView
GO

ALTER TABLE dbo.Airfield
	ADD Latitude DECIMAL(6, 4) NULL
		, Longitude DECIMAL(7, 4) NULL;

GO

UPDATE dbo.Airfield
	SET Latitude = 35.2264622
		, Longitude = -84.5849328
WHERE FaaId = '92A';

GO