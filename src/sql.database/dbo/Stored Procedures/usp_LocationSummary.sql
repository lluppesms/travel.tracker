-- =============================================
-- Stored Procedure: usp_LocationSummary
-- Description: Gets location summary for a user
-- =============================================
CREATE PROCEDURE [Travel].[usp_LocationSummary] (
  @UserName    nvarchar(128) = null
)
AS 
/*
Example Usage:
  EXEC [Travel].[usp_LocationSummary]
  EXEC [Travel].[usp_LocationSummary] @UserName = 'lyleluppes@microsoft.com'
*/
BEGIN

DECLARE @UserId int
DECLARE @places TABLE (
	[Name] [nvarchar](200),
	[TripName] [nvarchar](200) NULL,
	[LocationType] [nvarchar](100),
	[Address] [nvarchar](300),
	[City] [nvarchar](100),
	[State] [nvarchar](50),
	[Latitude] [float],
	[Longitude] [float],
	[StartDate] [date],
	[EndDate] [date] NULL,
	[Rating] [int],
	[Comments] [nvarchar](max)
)
DECLARE @types TABLE (
	[LocationType] [nvarchar](100)
)
IF @UserName IS NULL SET @UserName = 'lyleluppes@microsoft.com'

SELECT @UserId = Id FROM [Travel].[Users] WHERE Username = @UserName OR Email = @UserName
SELECT 'UserDefinition' as TableName, @UserId, u.Username, u.Email FROM [Travel].[Users] U WHERE Id = @UserId

INSERT INTO @places
SELECT l.Name, l.TripName, l.LocationType, l.Address, l.City, l.State, l.Latitude, l.Longitude, l.StartDate, l.EndDate, l.Rating, l.Comments
FROM [Travel].[Locations] l 
INNER JOIN [Travel].[Users] u ON l.UserId = u.Id 
WHERE l.UserId = @UserId
ORDER BY l.Longitude, u.Username, l.StartDate

UPDATE @places Set Comments = '' Where Comments = '0'

SELECT 'Locations_Visited' as TableName,* From @places

INSERT INTO @types
SELECT DISTINCT LocationType From @places

SELECT 'Location_Types_Visited' as TableName, p.LocationType, COUNT(*) 
FROM @places p INNER JOIN @types t ON p.LocationType = t.LocationType
GROUP BY p.LocationType

SELECT 'States_Visited' as TableName, MAX(State) as RowType, Count(*) as Counter FROM @places WHERE ISNULL(STATE,'') <> '' GROUP BY State ORDER BY State

SELECT 'National_Parks_List' as TableName, d.Name, d.State, dt.Name as DestinationType, CASE WHEN l.StartDate IS NULL THEN 'Not Visited' ELSE FORMAT(l.StartDate, 'MMM dd, yyyy') END as DateVisited
FROM [Travel].[Destinations] d INNER JOIN [Travel].[DestinationTypes] dt ON d.DestinationTypeId = dt.Id 
LEFT OUTER JOIN @places l ON l.Name = d.Name 
WHERE dt.Name = 'National Park'
ORDER BY d.Name

SELECT 'State_High_Points_List' as TableName, d.Name, d.State, dt.Name as DestinationType, CASE WHEN l.StartDate IS NULL THEN 'Not Visited' ELSE FORMAT(l.StartDate, 'MMM dd, yyyy') END as DateVisited
FROM [Travel].[Destinations] d INNER JOIN [Travel].[DestinationTypes] dt ON d.DestinationTypeId = dt.Id 
LEFT OUTER JOIN @places l ON l.Name = d.Name 
WHERE dt.Name = 'State High Point'
ORDER BY d.Name

SELECT 'Presidential_Libraries_List' as TableName, d.Name, d.State, dt.Name as DestinationType, CASE WHEN l.StartDate IS NULL THEN 'Not Visited' ELSE FORMAT(l.StartDate, 'MMM dd, yyyy') END as DateVisited
FROM [Travel].[Destinations] d INNER JOIN [Travel].[DestinationTypes] dt ON d.DestinationTypeId = dt.Id 
LEFT OUTER JOIN @places l ON l.Name = d.Name 
WHERE dt.Name = 'Presidential Library'
ORDER BY d.Name

END
GO
