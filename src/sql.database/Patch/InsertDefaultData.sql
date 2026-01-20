-- =============================================
-- Script: InsertDefaultData.sql
-- Description: Inserts default data for Travel Tracker
-- =============================================

-- Insert Location Types
IF NOT EXISTS (SELECT 1 FROM [dbo].[LocationTypes] WHERE [Name] = 'Restaurant')
BEGIN
    INSERT INTO [dbo].[LocationTypes] ([Name], [Description])
    VALUES ('Restaurant', 'Dining establishment')
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[LocationTypes] WHERE [Name] = 'Hotel')
BEGIN
    INSERT INTO [dbo].[LocationTypes] ([Name], [Description])
    VALUES ('Hotel', 'Lodging establishment')
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[LocationTypes] WHERE [Name] = 'Attraction')
BEGIN
    INSERT INTO [dbo].[LocationTypes] ([Name], [Description])
    VALUES ('Attraction', 'Tourist attraction or point of interest')
END

-- Insert Destination Types
IF NOT EXISTS (SELECT 1 FROM [dbo].[DestinationTypes] WHERE [Name] = 'National Park')
BEGIN
    INSERT INTO [dbo].[DestinationTypes] ([Name], [Description])
    VALUES ('National Park', 'US National Park')
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[DestinationTypes] WHERE [Name] = 'State High Point')
BEGIN
    INSERT INTO [dbo].[DestinationTypes] ([Name], [Description])
    VALUES ('State High Point', 'Highest point in a US state')
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[DestinationTypes] WHERE [Name] = 'Presidential Library')
BEGIN
    INSERT INTO [dbo].[DestinationTypes] ([Name], [Description])
    VALUES ('Presidential Library', 'US Presidential Library and Museum')
END

PRINT 'Default data inserted successfully'
GO
