
-- truncate table LocationType
-- go

BEGIN TRAN
GO

INSERT INTO LocationTypes (Name, Description) VALUES
('RV Park', 'RV Park or campground'),
('National Park', 'US National Park'),
('National Monument', 'US National Monument'),
('Harvest Host', 'Harvest Host location'),
('State Park', 'State Park'),
('Family', 'Family or friends location'),
('Other', 'Other location type')

SELECT * FROM LocationTypes

ROLLBACK
GO

SELECT * FROM LocationTypes
