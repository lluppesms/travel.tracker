CREATE TABLE [Travel].[AssistantActions]
(
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
GO

CREATE UNIQUE INDEX [IX_AssistantActions_UserId_ThreadId_CanonicalIdempotencyKey]
    ON [Travel].[AssistantActions] ([UserId], [ThreadId], [CanonicalIdempotencyKey]);
GO

CREATE INDEX [IX_AssistantActions_UserId_State_ExpiresAt]
    ON [Travel].[AssistantActions] ([UserId], [State], [ExpiresAt]);
GO

CREATE INDEX [IX_AssistantActions_State_RetainUntilDate]
    ON [Travel].[AssistantActions] ([State], [RetainUntilDate]);
GO
