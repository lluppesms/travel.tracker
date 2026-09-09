/*
delete FROM [Travel].[Destinations]
GO
delete FROM [Travel].[DestinationTypes]
GO
delete FROM [Travel].[locationTypes]
GO
delete FROM [Travel].[locations]
GO
*/

SELECT 'Users' as Table_Name, Count(*) as Row_Count FROM [Travel].[Users]
UNION
SELECT 'Destinations' as Table_Name, Count(*) as Row_Count FROM [Travel].[Destinations]
UNION
SELECT 'DestinationTypes' as Table_Name, Count(*) as Row_Count FROM [Travel].[DestinationTypes]
UNION
SELECT 'Locations' as Table_Name, Count(*) as Row_Count FROM [Travel].[Locations]
UNION
SELECT 'LocationTypes' as Table_Name, Count(*) as Row_Count FROM [Travel].[LocationTypes]

--SELECT * FROM [Travel].[Users]
--SELECT * FROM [Travel].[Locations]
--SELECT * FROM [Travel].[DestinationTypes]
--SELECT * FROM [Travel].[LocationTypes]

-- show every place I've visited
SELECT u.UserName, l.Name, l.LocationType, l.City, l.State, Cast(l.StartDate as varchar(10)) as Visited
FROM [Travel].[Locations] l inner join [Travel].[users] u on l.UserId = u.id 
--WHERE l.LocationType = 'National Park'
ORDER BY u.UserName, l.StartDate

---- show all destinations 
--SELECT dt.Name as DestType, d.* FROM [Travel].[Destinations] d 
--INNER JOIN [Travel].[DestinationTypes] dt ON d.DestinationTypeId = dt.Id

---- show all destinations with that I have visited
SELECT dt.Name as DestType, dt.Id as DestId, d.*, l.StartDate as DateVisited
From [Travel].[Destinations] d 
INNER JOIN [Travel].[DestinationTypes] dt ON d.DestinationTypeId = dt.Id
LEFT OUTER JOIN [Travel].[Locations] l on l.Name = d.Name 
WHERE l.StartDate IS NOT NULL
Order by d.DestinationTypeId, l.StartDate DESC
