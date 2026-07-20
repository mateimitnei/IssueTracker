DROP PROCEDURE IF EXISTS sp_CreateTicket;
DROP PROCEDURE IF EXISTS sp_UpdateTicket;
DROP PROCEDURE IF EXISTS sp_DeleteTicket;
DROP SEQUENCE IF EXISTS seq_TicketNumber;
GO

CREATE SEQUENCE seq_TicketNumber
    AS INT
    START WITH 105
    INCREMENT BY 1;
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
        
        -- Return the ticket id and key to the API
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id, @NewTicketKey AS TicketKey;
    
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
    @PriorityId INT = NULL,
    @StatusId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Ticket WHERE TicketKey = @TicketKey)
            THROW 50002, 'Ticket not found.', 1;

        IF @PriorityId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TicketPriority WHERE Id = @PriorityId)
            THROW 50003, 'Invalid priority type.', 1;
    
        IF @StatusId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TicketStatus WHERE Id = @StatusId)
            THROW 50004, 'Invalid status type.', 1;
    
        BEGIN TRAN;
            INSERT INTO TicketAudit (TicketKey, TicketTitle, TicketDescription, TicketStatusId, TicketPriorityId, TicketModifiedAt, TicketModificationType)
            SELECT TicketKey, Title, Description, StatusId, PriorityId, GETDATE(), 'UPDATE'
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
            INSERT INTO TicketAudit (TicketKey, TicketTitle, TicketDescription, TicketStatusId, TicketPriorityId, TicketModifiedAt, TicketModificationType)
            SELECT TicketKey, Title, Description, StatusId, PriorityId, GETDATE(), 'DELETE'
            FROM Ticket
            WHERE TicketKey = @TicketKey;
            
            DELETE FROM Ticket WHERE TicketKey = @TicketKey;  -- Delete AFTER audit
        COMMIT TRAN;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRAN;
        THROW;
    END CATCH
END;