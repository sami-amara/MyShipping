-- Seed data for TbShippmentStatus (customized)
-- Uses provided IDs: ShipmentId=2D20A738-C780-4548-8608-009F10F515EC
-- CarrierId=AC3F1B84-2147-4EE8-A28E-D99CBA230D97
-- CreatedBy=9CFD03AD-F0E1-46AB-A4FA-23F76788325B
-- Run this script in your Shipping database (SQL Server). Verify GUIDs exist where required.

SET NOCOUNT ON;
BEGIN TRANSACTION;

-- Deleted (0) - not linked to a shipment
IF NOT EXISTS (SELECT 1 FROM dbo.TbShippmentStatus WHERE Notes = 'Seed - Deleted')
BEGIN
    INSERT INTO dbo.TbShippmentStatus
    (Id, ShippmentId, Notes, CarrierId, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy, CurrentState)
    VALUES
    (NEWID(), NULL, 'Seed - Deleted', NULL, SYSUTCDATETIME(), '9CFD03AD-F0E1-46AB-A4FA-23F76788325B', NULL, NULL, 0);
END

-- Created (1)
IF NOT EXISTS (SELECT 1 FROM dbo.TbShippmentStatus WHERE Notes = 'Seed - Created')
BEGIN
    INSERT INTO dbo.TbShippmentStatus
    (Id, ShippmentId, Notes, CarrierId, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy, CurrentState)
    VALUES
    (NEWID(), 'A4595611-67E9-4913-8C9A-011090AACB87', 'Seed - Created', 'AC3F1B84-2147-4EE8-A28E-D99CBA230D97', SYSUTCDATETIME(), '9CFD03AD-F0E1-46AB-A4FA-23F76788325B', SYSUTCDATETIME(), '9CFD03AD-F0E1-46AB-A4FA-23F76788325B', 1);
END

-- Approved (2)
IF NOT EXISTS (SELECT 1 FROM dbo.TbShippmentStatus WHERE Notes = 'Seed - Approved')
BEGIN
    INSERT INTO dbo.TbShippmentStatus
    (Id, ShippmentId, Notes, CarrierId, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy, CurrentState)
    VALUES
    (NEWID(), '00171EFD-C070-4916-B6A2-0BBB9C897E45', 'Seed - Approved', 'AC3F1B84-2147-4EE8-A28E-D99CBA230D97', SYSUTCDATETIME(), '9CFD03AD-F0E1-46AB-A4FA-23F76788325B', SYSUTCDATETIME(), '9CFD03AD-F0E1-46AB-A4FA-23F76788325B', 2);
END

-- ReadyForShipping (3)
IF NOT EXISTS (SELECT 1 FROM dbo.TbShippmentStatus WHERE Notes = 'Seed - ReadyForShipping')
BEGIN
    INSERT INTO dbo.TbShippmentStatus
    (Id, ShippmentId, Notes, CarrierId, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy, CurrentState)
    VALUES
    (NEWID(), 'F5078035-ECAE-4958-B3A0-0BEA97F64D13', 'Seed - ReadyForShipping', 'AC3F1B84-2147-4EE8-A28E-D99CBA230D97', SYSUTCDATETIME(), '9CFD03AD-F0E1-46AB-A4FA-23F76788325B', SYSUTCDATETIME(), '9CFD03AD-F0E1-46AB-A4FA-23F76788325B', 3);
END

-- Shipped (4)
IF NOT EXISTS (SELECT 1 FROM dbo.TbShippmentStatus WHERE Notes = 'Seed - Shipped')
BEGIN
    INSERT INTO dbo.TbShippmentStatus
    (Id, ShippmentId, Notes, CarrierId, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy, CurrentState)
    VALUES
    (NEWID(), 'A7D2D725-C175-40CC-A449-130B2CC53C2D', 'Seed - Shipped', 'AC3F1B84-2147-4EE8-A28E-D99CBA230D97', SYSUTCDATETIME(), '9CFD03AD-F0E1-46AB-A4FA-23F76788325B', SYSUTCDATETIME(), '9CFD03AD-F0E1-46AB-A4FA-23F76788325B', 4);
END

-- Delivered (5)
IF NOT EXISTS (SELECT 1 FROM dbo.TbShippmentStatus WHERE Notes = 'Seed - Delivered')
BEGIN
    INSERT INTO dbo.TbShippmentStatus
    (Id, ShippmentId, Notes, CarrierId, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy, CurrentState)
    VALUES
    (NEWID(), '178E11A0-B4DF-4F76-95ED-1714C25BC521', 'Seed - Delivered', 'AC3F1B84-2147-4EE8-A28E-D99CBA230D97', SYSUTCDATETIME(), '9CFD03AD-F0E1-46AB-A4FA-23F76788325B', SYSUTCDATETIME(), '9CFD03AD-F0E1-46AB-A4FA-23F76788325B', 5);
END

-- Cancelled (6)
IF NOT EXISTS (SELECT 1 FROM dbo.TbShippmentStatus WHERE Notes = 'Seed - Cancelled')
BEGIN
    INSERT INTO dbo.TbShippmentStatus
    (Id, ShippmentId, Notes, CarrierId, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy, CurrentState)
    VALUES
    (NEWID(), '04538CF9-7CEC-4DC0-B2F3-199BBDDEA304', 'Seed - Cancelled', NULL, SYSUTCDATETIME(), '9CFD03AD-F0E1-46AB-A4FA-23F76788325B', SYSUTCDATETIME(), '9CFD03AD-F0E1-46AB-A4FA-23F76788325B', 6);
END

-- Returned (7)
IF NOT EXISTS (SELECT 1 FROM dbo.TbShippmentStatus WHERE Notes = 'Seed - Returned')
BEGIN
    INSERT INTO dbo.TbShippmentStatus
    (Id, ShippmentId, Notes, CarrierId, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy, CurrentState)
    VALUES
    (NEWID(), '0F8BCB2E-D60F-4148-B91E-1D0B06C7C811', 'Seed - Returned', 'AC3F1B84-2147-4EE8-A28E-D99CBA230D97', SYSUTCDATETIME(), '9CFD03AD-F0E1-46AB-A4FA-23F76788325B', SYSUTCDATETIME(), '9CFD03AD-F0E1-46AB-A4FA-23F76788325B', 7);
END

COMMIT TRANSACTION;

PRINT 'TbShippmentStatus seed completed. Remember to verify that provided GUIDs exist in your DB.'
