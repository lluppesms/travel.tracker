-- =============================================
-- Table: LocationTypes
-- Description: Stores location type definitions
-- =============================================
CREATE TABLE [Travel].[LocationTypes](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](500) NOT NULL,
 CONSTRAINT [PK_LocationTypes] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

-- Indexes
CREATE UNIQUE NONCLUSTERED INDEX [IX_LocationTypes_Name] ON [Travel].[LocationTypes]
([Name] ASC)
GO
