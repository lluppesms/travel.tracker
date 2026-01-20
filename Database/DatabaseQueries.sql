/*
truncate table nationalParks
go

truncate table locations
go

truncate table users
go

drop table nationalParks
go

drop table locations
go

drop table users
go

drop table dbo._EFMIgrationsHistory
go

*/

select 'Users' as Table_Name, Count(*) as Row_Count from Users
UNION
select 'Destinations' as Table_Name, Count(*) as Row_Count from Destinations
UNION
select 'DestinationTypes' as Table_Name, Count(*) as Row_Count from DestinationTypes
UNION
select 'Locations' as Table_Name, Count(*) as Row_Count from Locations
UNION
select 'LocationTypes' as Table_Name, Count(*) as Row_Count from LocationTypes


--select * from users
--select * from locations

-- show every place I've visited
Select u.UserName, l.Name, l.LocationType, l.City, l.State, l.ZipCode, l.StartDate
from Locations l inner join users u on l.UserId = u.id 
--Where l.LocationType = 'National Park'
order by u.UserName, l.StartDate

-- show all destinations 
Select dt.Name as DestType, d.* From Destinations d 
INNER JOIN DestinationTypes dt ON d.DestinationTypeId = dt.Id

-- show all destinations with that I have visited
Select dt.Name as DestType, d.*, l.StartDate as DateVisited
From Destinations d 
INNER JOIN DestinationTypes dt ON d.DestinationTypeId = dt.Id
LEFT OUTER JOIN Locations l on l.Name = d.Name 
