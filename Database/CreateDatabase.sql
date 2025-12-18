--USE [TravelTrackerDB]
--GO

CREATE TABLE [dbo].[Locations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Type] [nvarchar](50) NOT NULL,
	[UserId] [int] NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[TripName] [nvarchar](200) NULL,
	[LocationTypeId] [int] NULL,
	[LocationType] [nvarchar](100) NOT NULL,
	[Address] [nvarchar](300) NOT NULL,
	[City] [nvarchar](100) NOT NULL,
	[State] [nvarchar](50) NOT NULL,
	[ZipCode] [nvarchar](20) NOT NULL,
	[Latitude] [float] NOT NULL,
	[Longitude] [float] NOT NULL,
	[StartDate] [datetime2](7) NOT NULL,
	[EndDate] [datetime2](7) NULL,
	[Rating] [int] NOT NULL,
	[Comments] [nvarchar](max) NOT NULL,
	[TagsJson] [nvarchar](2000) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[ModifiedDate] [datetime2](7) NOT NULL
 CONSTRAINT [PK_Locations] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

CREATE TABLE [dbo].[LocationTypes](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](500) NOT NULL,
 CONSTRAINT [PK_LocationTypes] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

CREATE TABLE [dbo].[NationalParks](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Type] [nvarchar](50) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[State] [nvarchar](50) NOT NULL,
	[Latitude] [float] NOT NULL,
	[Longitude] [float] NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_NationalParks] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

CREATE TABLE [dbo].[Users](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Type] [nvarchar](50) NOT NULL,
	[Username] [nvarchar](200) NOT NULL,
	[Email] [nvarchar](200) NOT NULL,
	[EntraIdUserId] [nvarchar](50) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[LastLoginDate] [datetime2](7) NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

CREATE NONCLUSTERED INDEX [IX_Locations_LocationTypeId] ON [dbo].[Locations]
([LocationTypeId] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_Locations_StartDate] ON [dbo].[Locations]
([StartDate] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_Locations_State] ON [dbo].[Locations]
([State] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_Locations_UserId] ON [dbo].[Locations]
([UserId] ASC)
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_LocationTypes_Name] ON [dbo].[LocationTypes]
([Name] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_NationalParks_Name] ON [dbo].[NationalParks]
([Name] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_NationalParks_State] ON [dbo].[NationalParks]
([State] ASC)
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_ApiKey] ON [dbo].[Users]
([ApiKey] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_Users_Email] ON [dbo].[Users]
([Email] ASC)
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_EntraIdUserId] ON [dbo].[Users]
([EntraIdUserId] ASC)
GO

ALTER TABLE [dbo].[Locations] ADD  DEFAULT (N'[]') FOR [TagsJson]
GO
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF_Users_ApiKey]  DEFAULT (newid()) FOR [ApiKey]
GO
ALTER TABLE [dbo].[Locations]  WITH CHECK ADD  CONSTRAINT [FK_Locations_LocationTypes_LocationTypeId] FOREIGN KEY([LocationTypeId])
REFERENCES [dbo].[LocationTypes] ([Id])
GO
ALTER TABLE [dbo].[Locations] CHECK CONSTRAINT [FK_Locations_LocationTypes_LocationTypeId]
GO

CREATE PROCEDURE [dbo].[usp_LocationSummary] (
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
