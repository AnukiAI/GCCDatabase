SELECT COUNT(*) as TableCount FROM sys.tables WHERE type = 'U';
GO
SELECT name as TableName FROM sys.tables WHERE type = 'U' ORDER BY name;
GO
