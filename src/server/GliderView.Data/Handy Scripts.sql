
-- Get Flight Statistics
SELECT
	F.FlightId
	, F.StartDate
	, A.Description
	, S.StatisticId
	, S.Description
	, FS.Value
FROM
GliderView.dbo.Flight F
	JOIN GliderView.dbo.FlightStatistics FS
		ON F.FLightId = FS.FlightId
	LEFT JOIN GliderView.dbo.Aircraft A
		ON F.AircraftId = A.AircraftId
	JOIN GLiderView.dbo.Statistic S
		ON FS.StatisticId = S.StatisticId
WHERE F.StartDate > '2023-05-27'
	AND F.EndDate < '2023-05-28'
	AND FS.IsDeleted = 0
ORDER BY F.StartDate DESC, S.StatisticId


