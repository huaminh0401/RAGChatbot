USE RAG_Research_DB;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Users](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [FullName] [nvarchar](max) NULL,
        [Email] [nvarchar](256) NOT NULL,
        [PasswordHash] [nvarchar](max) NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL CONSTRAINT [DF_Users_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [IsLocked] [bit] NOT NULL CONSTRAINT [DF_Users_IsLocked] DEFAULT CONVERT(bit, 0),
        [IsApproved] [bit] NOT NULL CONSTRAINT [DF_Users_IsApproved] DEFAULT CONVERT(bit, 0),
        [Role] [nvarchar](50) NOT NULL CONSTRAINT [DF_Users_Role] DEFAULT N'Student',
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF COL_LENGTH('dbo.Users', 'CreatedAt') IS NULL
    ALTER TABLE dbo.Users ADD CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME();
GO

IF COL_LENGTH('dbo.Users', 'IsLocked') IS NULL
    ALTER TABLE dbo.Users ADD IsLocked bit NOT NULL CONSTRAINT DF_Users_IsLocked DEFAULT CONVERT(bit, 0);
GO

IF COL_LENGTH('dbo.Users', 'IsApproved') IS NULL
    ALTER TABLE dbo.Users ADD IsApproved bit NOT NULL CONSTRAINT DF_Users_IsApproved DEFAULT CONVERT(bit, 0);
GO

IF COL_LENGTH('dbo.Users', 'Role') IS NULL
    ALTER TABLE dbo.Users ADD [Role] nvarchar(50) NOT NULL CONSTRAINT DF_Users_Role DEFAULT N'Student';
GO

UPDATE dbo.Users
SET [Role] = CASE
    WHEN LOWER(LTRIM(RTRIM([Role]))) = N'admin' THEN N'Admin'
    WHEN LOWER(LTRIM(RTRIM([Role]))) = N'teacher' THEN N'Teacher'
    ELSE N'Student'
END
WHERE [Role] IS NULL
   OR [Role] NOT IN (N'Admin', N'Teacher', N'Student');
GO

UPDATE dbo.Users SET IsApproved = 1 WHERE [Role] = N'Admin';
GO

DECLARE @RoleDefaultConstraint nvarchar(256);
SELECT @RoleDefaultConstraint = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.default_object_id = dc.object_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Users')
  AND c.name = N'Role';

IF @RoleDefaultConstraint IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE dbo.Users DROP CONSTRAINT [' + @RoleDefaultConstraint + N']');
END
GO

IF EXISTS (
    SELECT Email
    FROM dbo.Users
    GROUP BY Email
    HAVING COUNT(*) > 1
)
BEGIN
    THROW 51000, 'Cannot create unique index: duplicate emails exist in dbo.Users. Remove or merge duplicate accounts first.', 1;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Users')
      AND name = N'Email'
      AND max_length = -1
)
BEGIN
    ALTER TABLE dbo.Users ALTER COLUMN Email nvarchar(256) NOT NULL;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Users')
      AND name = N'Role'
      AND max_length = -1
)
BEGIN
    ALTER TABLE dbo.Users ALTER COLUMN [Role] nvarchar(50) NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Users')
      AND c.name = N'Role'
)
BEGIN
    ALTER TABLE dbo.Users ADD CONSTRAINT DF_Users_Role DEFAULT N'Student' FOR [Role];
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Users')
      AND name = N'IX_Users_Email'
)
BEGIN
    CREATE UNIQUE INDEX IX_Users_Email ON dbo.Users(Email);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE [Role] = N'Admin')
BEGIN
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, CreatedAt, IsLocked, IsApproved, [Role])
    VALUES (
        N'Administrator',
        N'admin@rag.local',
        N'6G94qKPK8LYNjnTllCqm2G3BUM08AzOK7yW30tfjrMc=',
        SYSUTCDATETIME(),
        CONVERT(bit, 0),
        CONVERT(bit, 1),
        N'Admin'
    );
END
GO

SELECT Id, Email, FullName, Role, IsApproved, IsLocked, CreatedAt
FROM dbo.Users
ORDER BY Role, Email;
GO
