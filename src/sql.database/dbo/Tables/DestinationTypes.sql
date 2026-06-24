-- =============================================
-- Table: DestinationTypes
-- Description: Stores destination type definitions
-- =============================================
CREATE TABLE [Travel].[DestinationTypes](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](500) NOT NULL,
 CONSTRAINT [PK_DestinationTypes] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

-- Indexes
CREATE UNIQUE NONCLUSTERED INDEX [IX_DestinationTypes_Name] ON [Travel].[DestinationTypes]
([Name] ASC)
GO
