BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616193200_AddNormalizedSearchColumns'
)
BEGIN
    EXEC(N'ALTER TABLE [Tags] ADD [NormalizedName] AS LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Name], N'' '', N''''), NCHAR(9), N''''), NCHAR(10), N''''), NCHAR(13), N''''), N''™'', N''''), N''®'', N''''), N''©'', N''''), N''℠'', N''''), N''°'', N''''), N''-'', N''''), N''–'', N''''), N''—'', N''''), N''_'', N''''), N''.'', N''''), N'','', N''''), N'':'', N''''), N'';'', N''''), N''!'', N''''), N''?'', N''''), N'''''''', N''''), N''‘'', N''''), N''’'', N''''), N''`'', N''''), N''"'', N''''), N''“'', N''''), N''”'', N''''), N''('', N''''), N'')'', N''''), N''['', N''''), N'']'', N''''), N''{'', N''''), N''}'', N''''), N''/'', N''''), N''\'', N''''), N''|'', N''''), N''&'', N''''), N''+'', N''''), N''*'', N''''), N''@'', N''''), N''#'', N''''), N''~'', N''''), N''^'', N''''), N''='', N''''), N''<'', N''''), N''>'', N''''), N''%'', N'''')) PERSISTED');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616193200_AddNormalizedSearchColumns'
)
BEGIN
    EXEC(N'ALTER TABLE [Products] ADD [NormalizedName] AS LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Name], N'' '', N''''), NCHAR(9), N''''), NCHAR(10), N''''), NCHAR(13), N''''), N''™'', N''''), N''®'', N''''), N''©'', N''''), N''℠'', N''''), N''°'', N''''), N''-'', N''''), N''–'', N''''), N''—'', N''''), N''_'', N''''), N''.'', N''''), N'','', N''''), N'':'', N''''), N'';'', N''''), N''!'', N''''), N''?'', N''''), N'''''''', N''''), N''‘'', N''''), N''’'', N''''), N''`'', N''''), N''"'', N''''), N''“'', N''''), N''”'', N''''), N''('', N''''), N'')'', N''''), N''['', N''''), N'']'', N''''), N''{'', N''''), N''}'', N''''), N''/'', N''''), N''\'', N''''), N''|'', N''''), N''&'', N''''), N''+'', N''''), N''*'', N''''), N''@'', N''''), N''#'', N''''), N''~'', N''''), N''^'', N''''), N''='', N''''), N''<'', N''''), N''>'', N''''), N''%'', N'''')) PERSISTED');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616193200_AddNormalizedSearchColumns'
)
BEGIN
    EXEC(N'ALTER TABLE [ProductCategories] ADD [NormalizedName] AS LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Name], N'' '', N''''), NCHAR(9), N''''), NCHAR(10), N''''), NCHAR(13), N''''), N''™'', N''''), N''®'', N''''), N''©'', N''''), N''℠'', N''''), N''°'', N''''), N''-'', N''''), N''–'', N''''), N''—'', N''''), N''_'', N''''), N''.'', N''''), N'','', N''''), N'':'', N''''), N'';'', N''''), N''!'', N''''), N''?'', N''''), N'''''''', N''''), N''‘'', N''''), N''’'', N''''), N''`'', N''''), N''"'', N''''), N''“'', N''''), N''”'', N''''), N''('', N''''), N'')'', N''''), N''['', N''''), N'']'', N''''), N''{'', N''''), N''}'', N''''), N''/'', N''''), N''\'', N''''), N''|'', N''''), N''&'', N''''), N''+'', N''''), N''*'', N''''), N''@'', N''''), N''#'', N''''), N''~'', N''''), N''^'', N''''), N''='', N''''), N''<'', N''''), N''>'', N''''), N''%'', N'''')) PERSISTED');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616193200_AddNormalizedSearchColumns'
)
BEGIN
    EXEC(N'ALTER TABLE [Companies] ADD [NormalizedName] AS LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Name], N'' '', N''''), NCHAR(9), N''''), NCHAR(10), N''''), NCHAR(13), N''''), N''™'', N''''), N''®'', N''''), N''©'', N''''), N''℠'', N''''), N''°'', N''''), N''-'', N''''), N''–'', N''''), N''—'', N''''), N''_'', N''''), N''.'', N''''), N'','', N''''), N'':'', N''''), N'';'', N''''), N''!'', N''''), N''?'', N''''), N'''''''', N''''), N''‘'', N''''), N''’'', N''''), N''`'', N''''), N''"'', N''''), N''“'', N''''), N''”'', N''''), N''('', N''''), N'')'', N''''), N''['', N''''), N'']'', N''''), N''{'', N''''), N''}'', N''''), N''/'', N''''), N''\'', N''''), N''|'', N''''), N''&'', N''''), N''+'', N''''), N''*'', N''''), N''@'', N''''), N''#'', N''''), N''~'', N''''), N''^'', N''''), N''='', N''''), N''<'', N''''), N''>'', N''''), N''%'', N'''')) PERSISTED');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616193200_AddNormalizedSearchColumns'
)
BEGIN
    EXEC(N'ALTER TABLE [CertificationTypes] ADD [NormalizedName] AS LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Name], N'' '', N''''), NCHAR(9), N''''), NCHAR(10), N''''), NCHAR(13), N''''), N''™'', N''''), N''®'', N''''), N''©'', N''''), N''℠'', N''''), N''°'', N''''), N''-'', N''''), N''–'', N''''), N''—'', N''''), N''_'', N''''), N''.'', N''''), N'','', N''''), N'':'', N''''), N'';'', N''''), N''!'', N''''), N''?'', N''''), N'''''''', N''''), N''‘'', N''''), N''’'', N''''), N''`'', N''''), N''"'', N''''), N''“'', N''''), N''”'', N''''), N''('', N''''), N'')'', N''''), N''['', N''''), N'']'', N''''), N''{'', N''''), N''}'', N''''), N''/'', N''''), N''\'', N''''), N''|'', N''''), N''&'', N''''), N''+'', N''''), N''*'', N''''), N''@'', N''''), N''#'', N''''), N''~'', N''''), N''^'', N''''), N''='', N''''), N''<'', N''''), N''>'', N''''), N''%'', N'''')) PERSISTED');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616193200_AddNormalizedSearchColumns'
)
BEGIN
    EXEC(N'ALTER TABLE [Certificates] ADD [NormalizedCountry] AS LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Country], N'' '', N''''), NCHAR(9), N''''), NCHAR(10), N''''), NCHAR(13), N''''), N''™'', N''''), N''®'', N''''), N''©'', N''''), N''℠'', N''''), N''°'', N''''), N''-'', N''''), N''–'', N''''), N''—'', N''''), N''_'', N''''), N''.'', N''''), N'','', N''''), N'':'', N''''), N'';'', N''''), N''!'', N''''), N''?'', N''''), N'''''''', N''''), N''‘'', N''''), N''’'', N''''), N''`'', N''''), N''"'', N''''), N''“'', N''''), N''”'', N''''), N''('', N''''), N'')'', N''''), N''['', N''''), N'']'', N''''), N''{'', N''''), N''}'', N''''), N''/'', N''''), N''\'', N''''), N''|'', N''''), N''&'', N''''), N''+'', N''''), N''*'', N''''), N''@'', N''''), N''#'', N''''), N''~'', N''''), N''^'', N''''), N''='', N''''), N''<'', N''''), N''>'', N''''), N''%'', N'''')) PERSISTED');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616193200_AddNormalizedSearchColumns'
)
BEGIN
    EXEC(N'ALTER TABLE [Certificates] ADD [NormalizedLocation] AS LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Location], N'' '', N''''), NCHAR(9), N''''), NCHAR(10), N''''), NCHAR(13), N''''), N''™'', N''''), N''®'', N''''), N''©'', N''''), N''℠'', N''''), N''°'', N''''), N''-'', N''''), N''–'', N''''), N''—'', N''''), N''_'', N''''), N''.'', N''''), N'','', N''''), N'':'', N''''), N'';'', N''''), N''!'', N''''), N''?'', N''''), N'''''''', N''''), N''‘'', N''''), N''’'', N''''), N''`'', N''''), N''"'', N''''), N''“'', N''''), N''”'', N''''), N''('', N''''), N'')'', N''''), N''['', N''''), N'']'', N''''), N''{'', N''''), N''}'', N''''), N''/'', N''''), N''\'', N''''), N''|'', N''''), N''&'', N''''), N''+'', N''''), N''*'', N''''), N''@'', N''''), N''#'', N''''), N''~'', N''''), N''^'', N''''), N''='', N''''), N''<'', N''''), N''>'', N''''), N''%'', N'''')) PERSISTED');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616193200_AddNormalizedSearchColumns'
)
BEGIN
    EXEC(N'ALTER TABLE [Certificates] ADD [NormalizedNumber] AS LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Number], N'' '', N''''), NCHAR(9), N''''), NCHAR(10), N''''), NCHAR(13), N''''), N''™'', N''''), N''®'', N''''), N''©'', N''''), N''℠'', N''''), N''°'', N''''), N''-'', N''''), N''–'', N''''), N''—'', N''''), N''_'', N''''), N''.'', N''''), N'','', N''''), N'':'', N''''), N'';'', N''''), N''!'', N''''), N''?'', N''''), N'''''''', N''''), N''‘'', N''''), N''’'', N''''), N''`'', N''''), N''"'', N''''), N''“'', N''''), N''”'', N''''), N''('', N''''), N'')'', N''''), N''['', N''''), N'']'', N''''), N''{'', N''''), N''}'', N''''), N''/'', N''''), N''\'', N''''), N''|'', N''''), N''&'', N''''), N''+'', N''''), N''*'', N''''), N''@'', N''''), N''#'', N''''), N''~'', N''''), N''^'', N''''), N''='', N''''), N''<'', N''''), N''>'', N''''), N''%'', N'''')) PERSISTED');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616193200_AddNormalizedSearchColumns'
)
BEGIN
    CREATE INDEX [IX_Tags_NormalizedName] ON [Tags] ([NormalizedName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616193200_AddNormalizedSearchColumns'
)
BEGIN
    CREATE INDEX [IX_Products_NormalizedName] ON [Products] ([NormalizedName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616193200_AddNormalizedSearchColumns'
)
BEGIN
    CREATE INDEX [IX_ProductCategories_NormalizedName] ON [ProductCategories] ([NormalizedName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616193200_AddNormalizedSearchColumns'
)
BEGIN
    CREATE INDEX [IX_Companies_NormalizedName] ON [Companies] ([NormalizedName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616193200_AddNormalizedSearchColumns'
)
BEGIN
    CREATE INDEX [IX_CertificationTypes_NormalizedName] ON [CertificationTypes] ([NormalizedName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616193200_AddNormalizedSearchColumns'
)
BEGIN
    CREATE INDEX [IX_Certificates_NormalizedCountry] ON [Certificates] ([NormalizedCountry]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616193200_AddNormalizedSearchColumns'
)
BEGIN
    CREATE INDEX [IX_Certificates_NormalizedLocation] ON [Certificates] ([NormalizedLocation]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616193200_AddNormalizedSearchColumns'
)
BEGIN
    CREATE INDEX [IX_Certificates_NormalizedNumber] ON [Certificates] ([NormalizedNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616193200_AddNormalizedSearchColumns'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260616193200_AddNormalizedSearchColumns', N'8.0.11');
END;
GO

COMMIT;
GO

