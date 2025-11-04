ALTER PROCEDURE [dbo].[usp_LocationSummary] (
  @UserName    nvarchar(128) = null
)
AS 
/*
EXEC usp_LocationSummary
EXEC usp_LocationSummary @UserName = 'lyleluppes@microsoft.com'
*/

BEGIN

Declare @UserId int

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

select @UserId = Id from Users Where Username = @UserName or Email = @UserName
Select 'UserDefinition' as TableName@UserId, u.UserName, u.Email FROM Users U where Id = @UserId

INSERT INTO @places
SELECT l.Name, l.TripName, l.LocationType, l.Address, l.City, l.State, l.Latitude, l.Longitude, l.StartDate, l.EndDate, l.Rating, l.Comments
FROM Locations l 
INNER JOIN Users u on l.UserId = u.Id 
WHERE l.UserId = @UserId
ORDER BY l.Longitude, u.UserName, l.StartDate

UPDATE @places Set Comments = '' Where Comments = '0'

SELECT 'Locations_Visited' as TableName,* From @places

INSERT INTO @types
SELECT DISTINCT LocationType From @places

SELECT 'Location_Types_Visited' as TableName, p.LocationType, COUNT(*) FROM 
@places p INNER JOIN @types t on p.LocationType = t.LocationType
GROUP BY p.LocationType

SELECT 'States_Visited' as TableName, MAX(State) as RowType, Count(*) as Counter FROM @places WHERE ISNULL(STATE,'') <> '' GROUP BY State ORDER BY State

SELECT 'All_National_Parks_List' as TableName, Name, State from NationalParks Order by Name

END
GO

EXEC usp_LocationSummary
GO
