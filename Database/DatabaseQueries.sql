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
select 'NationalParks' as Table_Name, Count(*) as Row_Count from NationalParks
UNION
select 'Locations' as Table_Name, Count(*) as Row_Count from Locations
UNION
select 'LocationTypes' as Table_Name, Count(*) as Row_Count from LocationTypes

--select * from users
--
select * from locations

Select u.UserName, l.Name, l.LocationType, l.City, l.State, l.ZipCode, l.StartDate
from Locations l inner join users u on l.UserId = u.id 
order by u.UserName, l.StartDate
