DROP PROCEDURE IF EXISTS sp_CreateTicket;
DROP PROCEDURE IF EXISTS sp_UpdateTicket;
DROP PROCEDURE IF EXISTS sp_DeleteTicket;

GO



-- Create ticket --
CREATE PROCEDURE sp_CreateTicket
    @Title NVARCHAR(100),
    @Description NVARCHAR(1000),
    @PriorityId INT
AS
BEGIN
    SET NOCOUNT ON; -- To skip return messages like "X rows affected"

    BEGIN TRY
        
        IF NOT EXISTS (SELECT 1 FROM TicketPriority WHERE Id = @PriorityId)
            THROW 50001, 'Invalid priority type.', 1;

        DECLARE @NewTicketNumber INT = NEXT VALUE FOR seq_TicketNumber;
        DECLARE @NewTicketKey NVARCHAR(100) = CONCAT('TK-', @NewTicketNumber);

        BEGIN TRAN;
            INSERT INTO Ticket (TicketKey, Title, Description, PriorityId, StatusId, CreatedAt)
            VALUES (@NewTicketKey, @Title, @Description, @PriorityId, 1, GETDATE());
        COMMIT TRAN;
        
        -- Return the new ticket as a TicketDto
        SELECT
            t.Id,
            t.TicketKey,
            t.Title,
            t.Description,
            t.CreatedAt,
            s.Name AS Status,
            p.Name AS Priority
        FROM Ticket t
             JOIN TicketStatus s ON s.Id = t.StatusId
             JOIN TicketPriority p ON p.Id = t.PriorityId
        WHERE t.TicketKey = @NewTicketKey;
    
    END TRY
    BEGIN CATCH
        -- Rollback only if there was a transaction started
        IF @@TRANCOUNT > 0
            ROLLBACK TRAN;
        THROW;
    END CATCH
END;
GO

-- Update ticket --
CREATE PROCEDURE sp_UpdateTicket
    @TicketKey NVARCHAR(100),
    @Title NVARCHAR(100) = NULL,
    @Description NVARCHAR(1000) = NULL,
    @StatusId INT = NULL,
    @PriorityId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Ticket WHERE TicketKey = @TicketKey)
            THROW 50002, 'Ticket not found.', 1;
            
        IF @Title IS NOT NULL AND LEN(@Title) > 100
            THROW 50005, 'Title must be less than 100 characters.', 1;

        IF @PriorityId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TicketPriority WHERE Id = @PriorityId)
            THROW 50003, 'Invalid priority type.', 1;
    
        IF @StatusId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TicketStatus WHERE Id = @StatusId)
            THROW 50004, 'Invalid status type.', 1;
    
        BEGIN TRAN;
            INSERT INTO TicketAudit (TicketId, TicketKey, TicketTitle, TicketDescription, TicketStatusId, TicketPriorityId, TicketModifiedAt, TicketModificationType)
            SELECT Id, TicketKey, Title, Description, StatusId, PriorityId, GETDATE(), 'UPDATE'
            FROM Ticket
            WHERE TicketKey = @TicketKey;
        
            UPDATE Ticket
            SET
                Title = ISNULL(@Title, Title),
                Description = ISNULL(@Description, Description),
                PriorityId = ISNULL(@PriorityId, PriorityId),
                StatusId = ISNULL(@StatusId, StatusId)
            WHERE TicketKey = @TicketKey;
            
        COMMIT TRAN;

        SELECT
            t.Id,
            t.TicketKey,
            t.Title,
            t.Description,
            t.CreatedAt,
            s.Name AS Status,
            p.Name AS Priority
        FROM Ticket t
             JOIN TicketStatus s ON s.Id = t.StatusId
             JOIN TicketPriority p ON p.Id = t.PriorityId
        WHERE t.TicketKey = @TicketKey;

    END TRY
    BEGIN CATCH
        -- Rollback only if there was a transaction started
        IF @@TRANCOUNT > 0 
            ROLLBACK TRAN;
        THROW;
END CATCH
END;
GO

-- Delete ticket --
CREATE PROCEDURE sp_DeleteTicket
    @TicketKey NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Ticket WHERE TicketKey = @TicketKey)
            THROW 50002, 'Ticket not found.', 1;

        BEGIN TRAN;
            INSERT INTO TicketAudit (TicketId, TicketKey, TicketTitle, TicketDescription, TicketStatusId, TicketPriorityId, TicketModifiedAt, TicketModificationType)
            SELECT Id, TicketKey, Title, Description, StatusId, PriorityId, GETDATE(), 'DELETE'
            FROM Ticket
            WHERE TicketKey = @TicketKey;
            
            DELETE FROM Ticket WHERE TicketKey = @TicketKey;  -- Delete AFTER audit
        COMMIT TRAN;

        SELECT CAST(1 AS bit);
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRAN;
        THROW;
    END CATCH
END;