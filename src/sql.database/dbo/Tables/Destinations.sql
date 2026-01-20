-- =============================================
-- Table: Destinations
-- Description: Stores destination information
-- =============================================
CREATE TABLE [dbo].[Destinations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DestinationTypeId] [int] NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[State] [nvarchar](50) NOT NULL,
	[Latitude] [float] NOT NULL,
	[Longitude] [float] NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Destinations] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

-- Indexes
CREATE NONCLUSTERED INDEX [IX_Destinations_Name] ON [dbo].[Destinations]
([Name] ASC)
GO

CREATE NONCLUSTERED INDEX [IX_Destinations_State] ON [dbo].[Destinations]
([State] ASC)
GO

CREATE NONCLUSTERED INDEX [IX_Destinations_DestinationTypeId] ON [dbo].[Destinations]
([DestinationTypeId] ASC)
GO

-- Foreign Keys
ALTER TABLE [dbo].[Destinations]  WITH CHECK ADD  CONSTRAINT [FK_Destinations_DestinationTypes_DestinationTypeId] FOREIGN KEY([DestinationTypeId])
REFERENCES [dbo].[DestinationTypes] ([Id])
GO

ALTER TABLE [dbo].[Destinations] CHECK CONSTRAINT [FK_Destinations_DestinationTypes_DestinationTypeId]
GO
