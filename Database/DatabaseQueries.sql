/*
delete from [Travel].[Destinations]
GO
delete from [Travel].[DestinationTypes]
GO
delete from [Travel].[locationTypes]
GO
delete from [Travel].[locations]
GO
*/

select 'Users' as Table_Name, Count(*) as Row_Count from [Travel].[Users]
UNION
select 'Destinations' as Table_Name, Count(*) as Row_Count from [Travel].[Destinations]
UNION
select 'DestinationTypes' as Table_Name, Count(*) as Row_Count from [Travel].[DestinationTypes]
UNION
select 'Locations' as Table_Name, Count(*) as Row_Count from [Travel].[Locations]
UNION
select 'LocationTypes' as Table_Name, Count(*) as Row_Count from [Travel].[LocationTypes]


--select * from [Travel].[users]
--select * from [Travel].[locations]

-- show every place I've visited
Select u.UserName, l.Name, l.LocationType, l.City, l.State, l.ZipCode, l.StartDate
from [Travel].[Locations] l inner join [Travel].[users] u on l.UserId = u.id 
--Where l.LocationType = 'National Park'
order by u.UserName, l.StartDate

-- show all destinations 
Select dt.Name as DestType, d.* From [Travel].[Destinations] d 
INNER JOIN [Travel].[DestinationTypes] dt ON d.DestinationTypeId = dt.Id

-- show all destinations with that I have visited
Select dt.Name as DestType, d.*, l.StartDate as DateVisited
From [Travel].[Destinations] d 
INNER JOIN [Travel].[DestinationTypes] dt ON d.DestinationTypeId = dt.Id
LEFT OUTER JOIN [Travel].[Locations] l on l.Name = d.Name 
