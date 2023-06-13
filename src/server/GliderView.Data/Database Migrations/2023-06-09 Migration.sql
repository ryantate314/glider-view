USE GliderView
GO

GRANT DELETE ON dbo.Waypoint TO [gliderViewer]

GO

ALTER TABLE dbo.Flight
	ADD ModifiedDate DATETIME NULL;

GO