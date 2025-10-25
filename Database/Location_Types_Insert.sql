/*
delete from locationTypes
GO
truncate table LocationTypes
GO

DBCC CHECKIDENT ('LocationTypes', RESEED, 0)
*/

BEGIN TRAN
GO

INSERT INTO LocationTypes (Name, Description) VALUES
('RV Park', 'RV Park or campground'),
('National Park', 'US National Park'),
('National Monument', 'US National Monument'),
('National Memorial', 'US National Memorial'),
('National Military Battlefield', 'National Military Battlefield'),
('Harvest Host', 'Harvest Host location'),
('State Park', 'State Park'),
('Family', 'Family or friends location'),
('Presidential Library', 'Presidential Library'),
('Boondocking', 'Boondocking'),
('Other', 'Other location type')

SELECT * FROM LocationTypes

ROLLBACK
GO

SELECT * FROM LocationTypes
