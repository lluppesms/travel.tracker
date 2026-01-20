-- =============================================
-- Table: Users
-- Description: Stores user information
-- =============================================
CREATE TABLE [dbo].[Users](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Type] [nvarchar](50) NOT NULL,
	[Username] [nvarchar](200) NOT NULL,
	[Email] [nvarchar](200) NOT NULL,
	[EntraIdUserId] [nvarchar](50) NOT NULL,
	[ApiKey] [nvarchar](200) NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[LastLoginDate] [datetime2](7) NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

-- Default constraints
ALTER TABLE [dbo].[Users] ADD CONSTRAINT [DF_Users_ApiKey] DEFAULT (newid()) FOR [ApiKey]
GO

-- Indexes
CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_ApiKey] ON [dbo].[Users]
([ApiKey] ASC)
GO

CREATE NONCLUSTERED INDEX [IX_Users_Email] ON [dbo].[Users]
([Email] ASC)
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_EntraIdUserId] ON [dbo].[Users]
([EntraIdUserId] ASC)
GO
