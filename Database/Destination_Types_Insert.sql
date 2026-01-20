/*
delete from DestinationTypes
GO
truncate table DestinationTypes
GO

DBCC CHECKIDENT ('DestinationTypes', RESEED, 0)
*/

BEGIN TRAN
GO

INSERT INTO DestinationTypes (Name, Description) VALUES
('National Park', 'US National Parks'),
('State High Point', 'Highest point in each US state'),
('Presidential Library', 'Presidential Libraries and Museums')

SELECT * FROM DestinationTypes

ROLLBACK
GO

SELECT * FROM DestinationTypes
