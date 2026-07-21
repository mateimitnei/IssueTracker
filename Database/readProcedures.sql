-- View

DROP VIEW IF EXISTS vw_TicketDetails;
GO

CREATE VIEW vw_TicketDetails AS
SELECT
    Ticket.Id,
    Ticket.TicketKey,
    Ticket.Title,
    Ticket.Description,
    Ticket.CreatedAt,
    TicketStatus.Name AS Status,
    TicketPriority.Name AS Priority
FROM Ticket
INNER JOIN TicketStatus ON Ticket.StatusId = TicketStatus.Id
INNER JOIN TicketPriority ON Ticket.PriorityId = TicketPriority.Id;
GO

-- Stored Procedures

DROP PROCEDURE IF EXISTS sp_GetTicketByKey;
GO

CREATE PROCEDURE sp_GetTicketByKey
    @TicketKey NVARCHAR(100)
AS
BEGIN
    SELECT * FROM vw_TicketDetails
    WHERE TicketKey = @TicketKey;
END
GO

DROP PROCEDURE IF EXISTS sp_GetTicketAuditLog;
GO

CREATE PROCEDURE sp_GetTicketAuditLog
    @TicketKey NVARCHAR(100)
AS
BEGIN
    SELECT
        TicketAudit.Id,
        TicketAudit.TicketId,
        TicketAudit.TicketKey,
        TicketAudit.TicketTitle,
        TicketAudit.TicketDescription,
        TicketAudit.TicketModifiedAt,
        TicketAudit.TicketModificationType,
        TicketStatus.Name AS Status,
        TicketPriority.Name AS Priority
    FROM TicketAudit
    -- Using LEFT JOIN to comply with the requirement without losing data after a delete operation
    LEFT JOIN Ticket ON TicketAudit.TicketId = Ticket.Id
    LEFT JOIN TicketStatus ON TicketAudit.TicketStatusId = TicketStatus.Id
    LEFT JOIN TicketPriority ON TicketAudit.TicketPriorityId = TicketPriority.Id
    -- The lookup is performed using the TicketKey stored in the audit table, not the deleted record
    WHERE TicketAudit.TicketKey = @TicketKey
    ORDER BY TicketAudit.TicketModifiedAt DESC;
END
GO

DROP PROCEDURE IF EXISTS sp_GetTicketStats;
GO

CREATE PROCEDURE sp_GetTicketStats
AS
BEGIN
    SELECT
        TicketStatus.Name AS Status,
        TicketPriority.Name AS Priority,
        COUNT(Ticket.Id) AS TotalTickets
    FROM Ticket
    INNER JOIN TicketStatus ON Ticket.StatusId = TicketStatus.Id
    INNER JOIN TicketPriority ON Ticket.PriorityId = TicketPriority.Id
    GROUP BY 
        TicketStatus.Name, 
        TicketPriority.Name
    HAVING 
        COUNT(Ticket.Id) > 0;
END
GO

-- Indexes

DROP INDEX IF EXISTS ix_TicketTicketKey ON Ticket;
GO

-- The UNIQUE constraint on the Ticket table already creates an index automatically
CREATE UNIQUE NONCLUSTERED INDEX ix_TicketTicketKey
ON Ticket(TicketKey);
GO

DROP INDEX IF EXISTS ix_TicketAuditTicketId ON TicketAudit;
GO

CREATE NONCLUSTERED INDEX ix_TicketAuditTicketId
ON TicketAudit(TicketId);
GO