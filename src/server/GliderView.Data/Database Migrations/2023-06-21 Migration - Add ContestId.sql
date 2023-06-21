USE GliderView

ALTER TABLE dbo.Flight
	ADD ContestId VARCHAR(3) NULL;



/* ROLLBACK */

/*

ALTER TABLE dbo.Flight
	DROP COLUMN ContestId;

*/