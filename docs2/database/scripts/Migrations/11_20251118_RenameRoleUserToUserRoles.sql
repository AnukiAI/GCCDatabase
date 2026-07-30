BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    ALTER TABLE [RoleUser] DROP CONSTRAINT [FK_RoleUser_Roles_RolesId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    ALTER TABLE [RoleUser] DROP CONSTRAINT [FK_RoleUser_Users_UsersId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    ALTER TABLE [RoleUser] DROP CONSTRAINT [PK_RoleUser];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    EXEC sp_rename N'[RoleUser]', N'UserRoles';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    EXEC sp_rename N'[UserRoles].[IX_RoleUser_UsersId]', N'IX_UserRoles_UsersId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    ALTER TABLE [UserRoles] ADD CONSTRAINT [PK_UserRoles] PRIMARY KEY ([RolesId], [UsersId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    ALTER TABLE [UserRoles] ADD CONSTRAINT [FK_UserRoles_Roles_RolesId] FOREIGN KEY ([RolesId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    ALTER TABLE [UserRoles] ADD CONSTRAINT [FK_UserRoles_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251118214329_RenameRoleUserToUserRoles', N'8.0.11');
END;
GO

COMMIT;
GO

