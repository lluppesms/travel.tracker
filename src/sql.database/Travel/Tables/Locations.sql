-- =============================================
-- Table: Locations
-- Description: Stores location tracking information
-- =============================================
CREATE TABLE [Travel].[Locations](
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

-- Default constraints
ALTER TABLE [Travel].[Locations] ADD DEFAULT (N'[]') FOR [TagsJson]
GO

-- Indexes
CREATE NONCLUSTERED INDEX [IX_Locations_LocationTypeId] ON [Travel].[Locations]
([LocationTypeId] ASC)
GO

CREATE NONCLUSTERED INDEX [IX_Locations_StartDate] ON [Travel].[Locations]
([StartDate] ASC)
GO

CREATE NONCLUSTERED INDEX [IX_Locations_State] ON [Travel].[Locations]
([State] ASC)
GO

CREATE NONCLUSTERED INDEX [IX_Locations_UserId] ON [Travel].[Locations]
([UserId] ASC)
GO

-- Foreign Keys
ALTER TABLE [Travel].[Locations]  WITH CHECK ADD  CONSTRAINT [FK_Locations_LocationTypes_LocationTypeId] FOREIGN KEY([LocationTypeId])
REFERENCES [Travel].[LocationTypes] ([Id])
GO

ALTER TABLE [Travel].[Locations] CHECK CONSTRAINT [FK_Locations_LocationTypes_LocationTypeId]
GO
