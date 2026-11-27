CREATE OR ALTER TRIGGER dbo.TR_JOB_CARD_AUDIT
ON dbo.JOB_CARD
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @old nvarchar(max) = (SELECT * FROM deleted FOR JSON PATH);
    DECLARE @new nvarchar(max) = (SELECT * FROM inserted FOR JSON PATH);
    INSERT dbo.AUDIT_LOG (ActorUsername, Action, TableName, OccurredAt, OldValues, NewValues)
    VALUES (COALESCE(CONVERT(nvarchar(256), SESSION_CONTEXT(N'ActorUsername')), ORIGINAL_LOGIN()),
        CASE WHEN EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted) THEN 'UPDATE'
             WHEN EXISTS (SELECT 1 FROM inserted) THEN 'INSERT' ELSE 'DELETE' END,
        'JOB_CARD', SYSUTCDATETIME(), @old, @new);
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_INVOICE_AUDIT
ON dbo.INVOICE
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @old nvarchar(max) = (SELECT * FROM deleted FOR JSON PATH);
    DECLARE @new nvarchar(max) = (SELECT * FROM inserted FOR JSON PATH);
    INSERT dbo.AUDIT_LOG (ActorUsername, Action, TableName, OccurredAt, OldValues, NewValues)
    VALUES (COALESCE(CONVERT(nvarchar(256), SESSION_CONTEXT(N'ActorUsername')), ORIGINAL_LOGIN()),
        CASE WHEN EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted) THEN 'UPDATE'
             WHEN EXISTS (SELECT 1 FROM inserted) THEN 'INSERT' ELSE 'DELETE' END,
        'INVOICE', SYSUTCDATETIME(), @old, @new);
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_PAYMENT_AUDIT
ON dbo.PAYMENT
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @old nvarchar(max) = (SELECT * FROM deleted FOR JSON PATH);
    DECLARE @new nvarchar(max) = (SELECT * FROM inserted FOR JSON PATH);
    INSERT dbo.AUDIT_LOG (ActorUsername, Action, TableName, OccurredAt, OldValues, NewValues)
    VALUES (COALESCE(CONVERT(nvarchar(256), SESSION_CONTEXT(N'ActorUsername')), ORIGINAL_LOGIN()),
        CASE WHEN EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted) THEN 'UPDATE'
             WHEN EXISTS (SELECT 1 FROM inserted) THEN 'INSERT' ELSE 'DELETE' END,
        'PAYMENT', SYSUTCDATETIME(), @old, @new);
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_STOCK_AUDIT
ON dbo.STOCK
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @old nvarchar(max) = (SELECT * FROM deleted FOR JSON PATH);
    DECLARE @new nvarchar(max) = (SELECT * FROM inserted FOR JSON PATH);
    INSERT dbo.AUDIT_LOG (ActorUsername, Action, TableName, OccurredAt, OldValues, NewValues)
    VALUES (COALESCE(CONVERT(nvarchar(256), SESSION_CONTEXT(N'ActorUsername')), ORIGINAL_LOGIN()),
        CASE WHEN EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted) THEN 'UPDATE'
             WHEN EXISTS (SELECT 1 FROM inserted) THEN 'INSERT' ELSE 'DELETE' END,
        'STOCK', SYSUTCDATETIME(), @old, @new);
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_USER_ACCOUNT_AUDIT
ON dbo.USER_ACCOUNT
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @old nvarchar(max) =
        (SELECT Id, UserName, Email, FullName, PhoneNumber, IsActive FROM deleted FOR JSON PATH);
    DECLARE @new nvarchar(max) =
        (SELECT Id, UserName, Email, FullName, PhoneNumber, IsActive FROM inserted FOR JSON PATH);
    INSERT dbo.AUDIT_LOG (ActorUsername, Action, TableName, OccurredAt, OldValues, NewValues)
    VALUES (COALESCE(CONVERT(nvarchar(256), SESSION_CONTEXT(N'ActorUsername')), ORIGINAL_LOGIN()),
        CASE WHEN EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted) THEN 'UPDATE'
             WHEN EXISTS (SELECT 1 FROM inserted) THEN 'INSERT' ELSE 'DELETE' END,
        'USER_ACCOUNT', SYSUTCDATETIME(), @old, @new);
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_USER_ROLE_AUDIT
ON dbo.AspNetUserRoles
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @old nvarchar(max) = (SELECT UserId, RoleId FROM deleted FOR JSON PATH);
    DECLARE @new nvarchar(max) = (SELECT UserId, RoleId FROM inserted FOR JSON PATH);
    INSERT dbo.AUDIT_LOG (ActorUsername, Action, TableName, OccurredAt, OldValues, NewValues)
    VALUES (COALESCE(CONVERT(nvarchar(256), SESSION_CONTEXT(N'ActorUsername')), ORIGINAL_LOGIN()),
        CASE WHEN EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted) THEN 'UPDATE'
             WHEN EXISTS (SELECT 1 FROM inserted) THEN 'INSERT' ELSE 'DELETE' END,
        'USER_ROLE', SYSUTCDATETIME(), @old, @new);
END;
GO
