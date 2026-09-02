/*
=============================================================================
Travel Tracker - Create Schema, Tables, and Stored Procedures
=============================================================================
 Generated from the SQL project at src/sql.database.
 Objects are created in dependency order and each step is idempotent, so the
 script can safely be re-run against an existing database.

 Usage:
   sqlcmd -S <server> -d <database> -i CreateSchema.sql
=============================================================================
*/

-- USE [TravelTrackerDB]
-- GO

SET NOCOUNT ON;
GO

-- =============================================
-- Schema: Travel
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'Travel')
BEGIN
    EXEC (N'CREATE SCHEMA [Travel] AUTHORIZATION [dbo]');
END
GO

-- =============================================
-- Table: Travel.Users
-- Description: Stores user information
-- =============================================
IF OBJECT_ID(N'[Travel].[Users]', N'U') IS NULL
BEGIN
    CREATE TABLE [Travel].[Users](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Type] [nvarchar](50) NOT NULL,
        [Username] [nvarchar](200) NOT NULL,
        [Email] [nvarchar](200) NOT NULL,
        [EntraIdUserId] [nvarchar](50) NOT NULL,
        [ApiKey] [nvarchar](200) NULL CONSTRAINT [DF_Users_ApiKey] DEFAULT (newid()),
        [CreatedDate] [datetime2](7) NOT NULL,
        [LastLoginDate] [datetime2](7) NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_ApiKey' AND object_id = OBJECT_ID(N'[Travel].[Users]'))
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_ApiKey] ON [Travel].[Users] ([ApiKey] ASC) WHERE [ApiKey] IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_Email' AND object_id = OBJECT_ID(N'[Travel].[Users]'))
    CREATE NONCLUSTERED INDEX [IX_Users_Email] ON [Travel].[Users] ([Email] ASC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_EntraIdUserId' AND object_id = OBJECT_ID(N'[Travel].[Users]'))
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_EntraIdUserId] ON [Travel].[Users] ([EntraIdUserId] ASC);
GO

-- =============================================
-- Table: Travel.LocationTypes
-- Description: Stores location type definitions
-- =============================================
IF OBJECT_ID(N'[Travel].[LocationTypes]', N'U') IS NULL
BEGIN
    CREATE TABLE [Travel].[LocationTypes](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Name] [nvarchar](100) NOT NULL,
        [Description] [nvarchar](500) NOT NULL,
        CONSTRAINT [PK_LocationTypes] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LocationTypes_Name' AND object_id = OBJECT_ID(N'[Travel].[LocationTypes]'))
    CREATE UNIQUE NONCLUSTERED INDEX [IX_LocationTypes_Name] ON [Travel].[LocationTypes] ([Name] ASC);
GO

-- =============================================
-- Table: Travel.DestinationTypes
-- Description: Stores destination type definitions
-- =============================================
IF OBJECT_ID(N'[Travel].[DestinationTypes]', N'U') IS NULL
BEGIN
    CREATE TABLE [Travel].[DestinationTypes](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Name] [nvarchar](100) NOT NULL,
        [Description] [nvarchar](500) NOT NULL,
        CONSTRAINT [PK_DestinationTypes] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DestinationTypes_Name' AND object_id = OBJECT_ID(N'[Travel].[DestinationTypes]'))
    CREATE UNIQUE NONCLUSTERED INDEX [IX_DestinationTypes_Name] ON [Travel].[DestinationTypes] ([Name] ASC);
GO

-- =============================================
-- Table: Travel.Destinations
-- Description: Stores destination information
-- =============================================
IF OBJECT_ID(N'[Travel].[Destinations]', N'U') IS NULL
BEGIN
    CREATE TABLE [Travel].[Destinations](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [DestinationTypeId] [int] NOT NULL,
        [Name] [nvarchar](200) NOT NULL,
        [State] [nvarchar](50) NOT NULL,
        [Latitude] [float] NOT NULL,
        [Longitude] [float] NOT NULL,
        [Description] [nvarchar](max) NOT NULL,
        CONSTRAINT [PK_Destinations] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Destinations_DestinationTypes_DestinationTypeId] FOREIGN KEY ([DestinationTypeId])
            REFERENCES [Travel].[DestinationTypes] ([Id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Destinations_Name' AND object_id = OBJECT_ID(N'[Travel].[Destinations]'))
    CREATE NONCLUSTERED INDEX [IX_Destinations_Name] ON [Travel].[Destinations] ([Name] ASC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Destinations_State' AND object_id = OBJECT_ID(N'[Travel].[Destinations]'))
    CREATE NONCLUSTERED INDEX [IX_Destinations_State] ON [Travel].[Destinations] ([State] ASC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Destinations_DestinationTypeId' AND object_id = OBJECT_ID(N'[Travel].[Destinations]'))
    CREATE NONCLUSTERED INDEX [IX_Destinations_DestinationTypeId] ON [Travel].[Destinations] ([DestinationTypeId] ASC);
GO

-- =============================================
-- Table: Travel.AssistantActions
-- Description: Tracks pending/confirmed assistant-issued commands
-- =============================================
IF OBJECT_ID(N'[Travel].[AssistantActions]', N'U') IS NULL
BEGIN
    CREATE TABLE [Travel].[AssistantActions](
        [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_AssistantActions_Id] DEFAULT NEWSEQUENTIALID(),
        [UserId] INT NOT NULL,
        [ThreadId] NVARCHAR(200) NOT NULL,
        [ActionType] NVARCHAR(50) NOT NULL,
        [CommandSchemaVersion] INT NOT NULL,
        [State] NVARCHAR(20) NOT NULL,
        [CanonicalIdempotencyKey] CHAR(64) NOT NULL,
        [CanonicalCommandCiphertext] NVARCHAR(MAX) NULL,
        [PayloadHashSha256] BINARY(32) NOT NULL,
        [SanitizedSummary] NVARCHAR(400) NOT NULL,
        [ErrorCode] NVARCHAR(100) NULL,
        [CreatedLocationId] INT NULL,
        [CreatedDate] DATETIME2(7) NOT NULL,
        [ModifiedDate] DATETIME2(7) NOT NULL,
        [ExpiresAt] DATETIME2(7) NOT NULL,
        [CompletedDate] DATETIME2(7) NULL,
        [RetainUntilDate] DATETIME2(7) NOT NULL,
        [RowVersion] ROWVERSION NOT NULL,
        CONSTRAINT [PK_AssistantActions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_AssistantActions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Travel].[Users] ([Id]),
        CONSTRAINT [CK_AssistantActions_State] CHECK ([State] IN (N'Pending', N'Executing', N'Confirmed', N'Cancelled', N'Expired', N'Failed'))
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AssistantActions_UserId_ThreadId_CanonicalIdempotencyKey' AND object_id = OBJECT_ID(N'[Travel].[AssistantActions]'))
    CREATE UNIQUE INDEX [IX_AssistantActions_UserId_ThreadId_CanonicalIdempotencyKey]
        ON [Travel].[AssistantActions] ([UserId], [ThreadId], [CanonicalIdempotencyKey]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AssistantActions_UserId_State_ExpiresAt' AND object_id = OBJECT_ID(N'[Travel].[AssistantActions]'))
    CREATE INDEX [IX_AssistantActions_UserId_State_ExpiresAt]
        ON [Travel].[AssistantActions] ([UserId], [State], [ExpiresAt]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AssistantActions_State_RetainUntilDate' AND object_id = OBJECT_ID(N'[Travel].[AssistantActions]'))
    CREATE INDEX [IX_AssistantActions_State_RetainUntilDate]
        ON [Travel].[AssistantActions] ([State], [RetainUntilDate]);
GO

-- =============================================
-- Table: Travel.Locations
-- Description: Stores location tracking information
-- =============================================
IF OBJECT_ID(N'[Travel].[Locations]', N'U') IS NULL
BEGIN
    CREATE TABLE [Travel].[Locations](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Type] [nvarchar](50) NOT NULL,
        [UserId] [int] NOT NULL,
        [Name] [nvarchar](200) NOT NULL,
        [TripName] [nvarchar](200) NULL,
        [LocationTypeId] [int] NULL,
        [AssistantActionId] [uniqueidentifier] NULL,
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
        [TagsJson] [nvarchar](2000) NOT NULL CONSTRAINT [DF_Locations_TagsJson] DEFAULT (N'[]'),
        [CreatedDate] [datetime2](7) NOT NULL,
        [ModifiedDate] [datetime2](7) NOT NULL,
        CONSTRAINT [PK_Locations] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Locations_LocationTypes_LocationTypeId] FOREIGN KEY ([LocationTypeId])
            REFERENCES [Travel].[LocationTypes] ([Id]),
        CONSTRAINT [FK_Locations_AssistantActions_AssistantActionId] FOREIGN KEY ([AssistantActionId])
            REFERENCES [Travel].[AssistantActions] ([Id]) ON DELETE SET NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Locations_LocationTypeId' AND object_id = OBJECT_ID(N'[Travel].[Locations]'))
    CREATE NONCLUSTERED INDEX [IX_Locations_LocationTypeId] ON [Travel].[Locations] ([LocationTypeId] ASC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Locations_StartDate' AND object_id = OBJECT_ID(N'[Travel].[Locations]'))
    CREATE NONCLUSTERED INDEX [IX_Locations_StartDate] ON [Travel].[Locations] ([StartDate] ASC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Locations_State' AND object_id = OBJECT_ID(N'[Travel].[Locations]'))
    CREATE NONCLUSTERED INDEX [IX_Locations_State] ON [Travel].[Locations] ([State] ASC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Locations_UserId' AND object_id = OBJECT_ID(N'[Travel].[Locations]'))
    CREATE NONCLUSTERED INDEX [IX_Locations_UserId] ON [Travel].[Locations] ([UserId] ASC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Locations_AssistantActionId' AND object_id = OBJECT_ID(N'[Travel].[Locations]'))
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Locations_AssistantActionId] ON [Travel].[Locations] ([AssistantActionId] ASC)
        WHERE [AssistantActionId] IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Locations_UserId_StartDate_Name' AND object_id = OBJECT_ID(N'[Travel].[Locations]'))
    CREATE NONCLUSTERED INDEX [IX_Locations_UserId_StartDate_Name] ON [Travel].[Locations] ([UserId] ASC, [StartDate] ASC, [Name] ASC);
GO

-- =============================================
-- Stored Procedure: Travel.usp_LocationSummary
-- Description: Gets location summary for a user
-- =============================================
IF OBJECT_ID(N'[Travel].[usp_LocationSummary]', N'P') IS NULL
    EXEC (N'CREATE PROCEDURE [Travel].[usp_LocationSummary] AS SET NOCOUNT ON;');
GO

ALTER PROCEDURE [Travel].[usp_LocationSummary] (
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

PRINT 'Travel Tracker schema creation complete.';
GO
