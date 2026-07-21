IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'IssueTrackerDb')
BEGIN
    CREATE DATABASE IssueTrackerDb;
END
GO

USE IssueTrackerDb;
GO

-- Create and populate lookup tables

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TicketStatus')
BEGIN
    CREATE TABLE TicketStatus (
        Id TINYINT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(20) NOT NULL
    );
    
    INSERT INTO TicketStatus (Name) 
    VALUES ('TODO'), ('IN PROGRESS'), ('IN REVIEW'), ('DONE');
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TicketPriority')
BEGIN
    CREATE TABLE TicketPriority (
        Id TINYINT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(20) NOT NULL
    );

    INSERT INTO TicketPriority (Name) 
    VALUES ('LOW'), ('MEDIUM'), ('HIGH');
END
GO

-- Create main tables

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Ticket')
BEGIN
    CREATE TABLE Ticket (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TicketKey NVARCHAR(100) UNIQUE,
        Title NVARCHAR(100) NOT NULL,
        Description NVARCHAR(1000),
        CreatedAt DATETIME2 NOT NULL,
        StatusId TINYINT NOT NULL DEFAULT 1,
        PriorityId TINYINT NOT NULL DEFAULT 2,
        
        FOREIGN KEY (StatusId) REFERENCES TicketStatus(Id),
        FOREIGN KEY (PriorityId) REFERENCES TicketPriority(Id)
    );
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TicketAudit')
BEGIN
    CREATE TABLE TicketAudit (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TicketId INT NOT NULL, -- TicketId cannot be UNIQUE; otherwise, each ticket could only have a single entry in the Audit table
        TicketKey NVARCHAR(100),
        TicketTitle NVARCHAR(100) NOT NULL,
        TicketDescription NVARCHAR(1000), -- New column
        TicketStatusId TINYINT NOT NULL DEFAULT 1,
        TicketPriorityId TINYINT NOT NULL DEFAULT 2,
        TicketModifiedAt DATETIME2 NOT NULL,
        TicketModificationType NVARCHAR(20) NOT NULL, -- New column
    
        FOREIGN KEY (TicketId) REFERENCES Ticket(Id),
        FOREIGN KEY (TicketStatusId) REFERENCES TicketStatus(Id),
        FOREIGN KEY (TicketPriorityId) REFERENCES TicketPriority(Id)
    );
END
GO

-- Insert mock data

-- Add tickets only if the table is empty
IF NOT EXISTS (SELECT 1 FROM Ticket)
BEGIN
    INSERT INTO Ticket (TicketKey, Title, Description, CreatedAt, StatusId, PriorityId)
    VALUES 
    ('TK-101', 'Setare structura initiala', 'Creare foldere, straturi si solutie', GETDATE(), 4, 3),
    ('TK-102', 'Creare endpoint POST', 'Creare DTO si logica adaugare', GETDATE(), 2, 2),
    ('TK-103', 'Implementare endpoint PATCH', 'Actualizare campuri si scriere in istoric', GETDATE(), 1, 2),
    ('TK-104', 'Scriere script SQL', 'Adaugare date mock', GETDATE(), 3, 1);
END
GO

-- Add audit history only if the table is completely empty
IF NOT EXISTS (SELECT 1 FROM TicketAudit)
BEGIN
    INSERT INTO TicketAudit (TicketId, TicketKey, TicketTitle, TicketDescription, TicketStatusId, TicketPriorityId, TicketModifiedAt, TicketModificationType)
    VALUES 
    (1, 'TK-101', 'Setare structura initiala', 'Creare foldere, straturi si solutie', 1, 3, DATEADD(day, -2, GETDATE()), 'UPDATE'),
    (1, 'TK-101', 'Setare structura initiala', 'Creare foldere, straturi si solutie', 2, 3, DATEADD(day, -1, GETDATE()), 'UPDATE');
END
GO