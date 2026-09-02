/*
=============================================================================
 Travel Tracker - Drop Schema and All Objects
=============================================================================
 Removes every object in the [Travel] schema and then the schema itself.
 Intended for cleanly removing Travel Tracker from a SHARED database.

 !!! WARNING: THIS PERMANENTLY DELETES ALL TRAVEL TRACKER DATA !!!

 Safety: this script will not run until you opt in. Change the @ConfirmDrop
 value below from 0 to 1, then execute.

 Usage:
   sqlcmd -S <server> -d <database> -i DropSchema.sql
=============================================================================
*/

-- USE [TravelTrackerDB]
-- GO

SET NOCOUNT ON;
GO

-- ***** SET THIS TO 1 TO ACTUALLY DROP EVERYTHING *****
DROP TABLE IF EXISTS #DropOptions;
CREATE TABLE #DropOptions (ConfirmDrop bit NOT NULL);
INSERT INTO #DropOptions (ConfirmDrop) VALUES (0);
GO

IF NOT EXISTS (SELECT 1 FROM #DropOptions WHERE ConfirmDrop = 1)
BEGIN
    RAISERROR(N'ABORTED: Set ConfirmDrop to 1 near the top of this script to drop the [Travel] schema and all its data.', 16, 1);
    SET NOEXEC ON;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'Travel')
BEGIN
    PRINT 'Schema [Travel] does not exist - nothing to drop.';
    SET NOEXEC ON;
END
GO

-- =============================================
-- Stored Procedures
-- =============================================
PRINT 'Dropping stored procedures...';
GO

DROP PROCEDURE IF EXISTS [Travel].[usp_LocationSummary];
GO

-- =============================================
-- Foreign keys
-- Dropped up front so the tables can be removed in any order.
-- =============================================
PRINT 'Dropping foreign keys...';
GO

DECLARE @sql nvarchar(max) = N'';

SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
                   + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(13)
FROM sys.foreign_keys fk
INNER JOIN sys.tables t ON fk.parent_object_id = t.object_id
WHERE SCHEMA_NAME(t.schema_id) = N'Travel'
   OR fk.referenced_object_id IN (SELECT object_id FROM sys.tables WHERE SCHEMA_NAME(schema_id) = N'Travel');

IF @sql <> N'' EXEC sp_executesql @sql;
GO

-- =============================================
-- Tables (child-to-parent order)
-- =============================================
PRINT 'Dropping tables...';
GO

DROP TABLE IF EXISTS [Travel].[Locations];
GO
DROP TABLE IF EXISTS [Travel].[AssistantActions];
GO
DROP TABLE IF EXISTS [Travel].[Destinations];
GO
DROP TABLE IF EXISTS [Travel].[DestinationTypes];
GO
DROP TABLE IF EXISTS [Travel].[LocationTypes];
GO
DROP TABLE IF EXISTS [Travel].[Users];
GO

-- =============================================
-- Catch any remaining objects added after this script was written
-- =============================================
PRINT 'Dropping any remaining objects in [Travel]...';
GO

DECLARE @sql nvarchar(max) = N'';

-- Views, procedures, functions, then tables/synonyms/types
SELECT @sql = @sql
    + CASE o.type
        WHEN 'V'  THEN N'DROP VIEW '
        WHEN 'P'  THEN N'DROP PROCEDURE '
        WHEN 'FN' THEN N'DROP FUNCTION '
        WHEN 'IF' THEN N'DROP FUNCTION '
        WHEN 'TF' THEN N'DROP FUNCTION '
        WHEN 'SN' THEN N'DROP SYNONYM '
        WHEN 'U'  THEN N'DROP TABLE '
      END
    + QUOTENAME(SCHEMA_NAME(o.schema_id)) + N'.' + QUOTENAME(o.name) + N';' + CHAR(13)
FROM sys.objects o
WHERE SCHEMA_NAME(o.schema_id) = N'Travel'
  AND o.type IN ('V', 'P', 'FN', 'IF', 'TF', 'SN', 'U')
  AND o.is_ms_shipped = 0
ORDER BY CASE o.type WHEN 'V' THEN 1 WHEN 'P' THEN 2 WHEN 'SN' THEN 3 WHEN 'U' THEN 5 ELSE 4 END;

IF @sql <> N'' EXEC sp_executesql @sql;
GO

-- User-defined types
DECLARE @sql nvarchar(max) = N'';

SELECT @sql = @sql + N'DROP TYPE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name) + N';' + CHAR(13)
FROM sys.types t
WHERE t.is_user_defined = 1
  AND SCHEMA_NAME(t.schema_id) = N'Travel';

IF @sql <> N'' EXEC sp_executesql @sql;
GO

-- =============================================
-- Schema
-- =============================================
PRINT 'Dropping schema [Travel]...';
GO

DROP SCHEMA IF EXISTS [Travel];
GO

PRINT 'Travel Tracker schema removal complete.';
GO

SET NOEXEC OFF;
GO

DROP TABLE IF EXISTS #DropOptions;
GO
