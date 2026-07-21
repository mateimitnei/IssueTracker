# Issue Tracker Web API — Ticket Management System

## Requirements

- .NET 10.0 SDK
- Microsoft SQL Server 2025

## How to run the project

1. Create a new SQL Server.
2. Run the following database init and stored procedures scripts: [Database init](Database/InitTables.sql), 
[Read Procedures](Database/ReadProcedures.sql), [Write Procedures](Database/WriteProcedures.sql).
3. Fill in the `DefaultConnection` string in `appsettings.json` with the connection string for your local SQL Server.
4. Run the `API` project using `dotnet run` or through your IDE.

# Project Specification

## 1. Technologies and Architecture

- **Framework:** .NET Core using Minimal API.
- **API Documentation:** Swagger integration for easy calling of endpoints.
- **Code Organization:** Structuring endpoints using static classes.
- **Error Handling:** Implementation of a centralized system.

## 2. Lookup Tables

### TicketStatus (byte)

| Value | Meaning |
|---------|--------------|
| `1`     | TO DO        |
| `2`     | IN PROGRESS  |
| `3`     | IN REVIEW    |
| `4`     | DONE         |

### TicketPriority (byte)

| Value | Meaning |
|---------|--------------|
| `1`     | LOW          |
| `2`     | MEDIUM       |
| `3`     | HIGH         |

## 3. API Endpoints

### 3.1. Add Ticket — `POST`

Endpoint for adding an issue to the database.

- **`Ticket` Entity (in DB):** `Id` (int, identity), `TicketKey` (string), `Title` (string), `Description` (string), `CreatedAt` (DateTime), `StatusId` (byte), `PriorityId` (byte).
- **DTO (Data Transfer Object):** The user sends only `Title`, `Description`, and `PriorityId`.
- **Validations and saving rules:**
    - `Title`: cannot be `NULL`/`EMPTY` and must not exceed 100 characters.
    - `TicketKey`: unique string in the system (generated from code or validated as unique, e.g.: `"TK-101"`).
    - `CreatedAt`: automatically set at the time of processing (not requested from the user).
    - `StatusId`: automatically set to the value `1` (TO DO).
- **Errors:** for an invalid priority, a `400 Bad Request` is returned using the standard `ProblemDetails` format.

### 3.2. Edit Ticket — `PATCH`

Endpoint for editing a ticket, uniquely identified by the `TicketKey` received as a parameter in the URL path.

- **Strict rule:** the `TicketKey` and `CreatedAt` properties CANNOT be edited.

### 3.3. Get All Tickets — `GET`

Endpoint to retrieve the complete list of saved tickets from the database.

### 3.4. Get Specific Ticket — `GET`

Endpoint to return a single ticket, based on the `TicketKey` from the path.

- **Errors:** if the ticket is not found, the application returns `404 Not Found` (according to the REST contract).

### 3.5. Delete Ticket — `DELETE`

Endpoint that deletes an existing ticket, using the `TicketKey` from the path as an identifier.

## 4. Audit Requirements (History)

- **Monitoring:** any modification or deletion of a ticket requires saving the previous version in a log table (e.g.: `TicketAudit`).
- **Retained data:** the table will record the old data (e.g.: `OldStatusId`, `OldTitle`, `ModificationDate`).
- **Dedicated endpoint (`GET`):** allows viewing the log (history) for a specific `TicketKey` received in the path.

## 5. Database

- **System:** SQL Server.
- **Required tables:** `TicketStatus`, `TicketPriority`, `Ticket`, `TicketAudit`.
- **Relationships:** the tables must be linked by Foreign Keys according to the structure.
- **Approach (Database-First):** it is encouraged to manually write SQL scripts, directly applying constraints such as `UNIQUE`, `NOT NULL`, and `DEFAULT` at the database level.

---

# Additional Requirement — Ticket Management API

## Context and Objective

The initial requirement (Minimal API with Route Groups, Global Error Handling and Swagger, the entities `Ticket`, `TicketStatus`, `TicketPriority`, `TicketAudit`) remains unchanged as a functional base. This document divides it into **two parallel workflows**.

Besides the functional requirements, each workflow contains explicit SQL requirements — stored procedures, joins, views, transactions, and indexes.

## Common Architecture

1. The .NET solution structure, with a Web API project and separate folders for `endpoints`, `dtos`, `data`, and `middleware`.
2. The initial SQL script for creating the database, which contains the reference tables `TicketStatus` and `TicketPriority`, populated with the fixed values from the requirement.
3. The database connection convention — EF Core Database First.
4. The Global Error Handling middleware, a single handler used by both, which maps exceptions to `ProblemDetails` with the codes `400`, `404`, `409`, and `500`.
5. The Swagger configuration, common for all endpoints, regardless of who writes them.

## A — Ticket Lifecycle (Create, Update, Delete)

A is responsible for everything related to **writing** to the database.

### Endpoints A

- **Endpoint 1 — `POST tickets`:** ticket creation from a DTO containing `Title`, `Description`, and `PriorityId`.
    - Validates `Title` (not null/empty, max 100 characters).
    - Generates a unique `TicketKey`.
    - `StatusId` is automatically set to `1`, and `CreatedAt` to the current date and time.
    - Invalid `PriorityId` → `400 Bad Request` (ProblemDetails).
- **Endpoint 2 — `PATCH tickets/{TicketKey}`:** partial editing of the ticket. `TicketKey` and `CreatedAt` are not editable — if they come in the body, they are either ignored, or a `400` is returned.
- **Endpoint 5 — `DELETE tickets/{TicketKey}`:** deletion of an existing ticket. If it doesn't exist → `404`.

### SQL Requirements — A

1. **`sp_CreateTicket`** — parameters: `Title`, `Description`, `PriorityId`.
    - Generates a unique `TicketKey` directly inside the procedure (e.g.: SQL sequence or logic based on the maximum existing number — the choice will be discussed and argued).
    - Validates `PriorityId` through an `EXISTS` on the `TicketPriority` table; if the priority does not exist, it throws a custom error that the API translates into `400`.
    - Inserts the ticket with `StatusId = 1` and automatic `CreatedAt`.
    - Uses an **explicit transaction** (`BEGIN TRAN` / `COMMIT`), because inserting the ticket and the possible first audit record must be atomic.

2. **`sp_UpdateTicket`** — parameters: `TicketKey` and, optionally, `Title`, `Description`, `PriorityId`, `StatusId` (partial update).
    - Uses `TRY/CATCH` blocks with `THROW`/`RAISERROR` for a non-existent ticket or invalid status/priority values.
    - Before the update, it reads the old row and inserts it into `TicketAudit`.
    - The update and the insertion into the audit are done within an **explicit transaction**.
    - *Advanced level:* the `OUTPUT` clause can be used directly in the `UPDATE` to populate the audit without a separate select.

3. **`sp_DeleteTicket`** — deletes the ticket based on the `TicketKey`.
    - If physical deletion is chosen, the last version of the ticket must be saved in `TicketAudit` **before** the `DELETE`, otherwise the history is lost.

### Database level constraints (independent of C# code)

- `UNIQUE` on the `TicketKey` column from the `Ticket` table.
- `NOT NULL` on `Title`, `StatusId`, `PriorityId`, `CreatedAt`.
- Default value (`DEFAULT`) for `CreatedAt` = current date.
- Default value `1` for `StatusId`.
- Foreign keys from `Ticket` to `TicketStatus` and `TicketPriority`.
- *(optional)* `CHECK` for the maximum length of the title.

## B — Read, Audit and Reporting

B is responsible for everything related to **reading, aggregation, and history**.

### Endpoints B

- **Endpoint 3 — `GET tickets`:** retrieves all tickets, with the status and priority displayed as text (not just raw numeric values).
- **Endpoint 4 — `GET tickets/{TicketKey}`:** retrieves a single ticket. If it doesn't exist → `404`.
- **Endpoint 7 — `GET tickets/{TicketKey}/audit`:** retrieves the complete modification history for a ticket, chronologically ordered.

### SQL Requirements — B

1. **`vw_TicketDetails`** (view) — selects the ticket data and merges it, via an `INNER JOIN`, with the status name from `TicketStatus` and the priority name from `TicketPriority`. Used by endpoints 3 and 4; the main reason is avoiding the exposure of raw ids to the client without translation into text.

2. **`sp_GetTicketByKey`** — parameter: `TicketKey`; selects from the view above. If it finds nothing, the decision to return `404` is made in the C# code, not in the procedure.

3. **`sp_GetTicketAuditLog`** — parameter: `TicketKey`; does a `JOIN` between `TicketAudit` and `Ticket`, plus a `LEFT JOIN` to `TicketStatus` and `TicketPriority` to translate the old values saved in the audit. A good exercise for `LEFT JOIN` with multiple aliases on the same table, because both the old status and the old priority must be translated separately.

4. **Indexing** (must be justified, not just written):
    - `UNIQUE` index on `TicketKey` from `Ticket` — supports both uniqueness and the searches from endpoints 4 and 7.
    - Index on the column that links `TicketAudit` to `Ticket`, because `sp_GetTicketAuditLog` filters by this column.
    - *Oral evaluation question:* why isn't an index also placed on `Title` or `Description`?

5. *(optional, bonus)* Aggregation query with `GROUP BY` and `COUNT` — the number of tickets for each status and each priority. It is not explicitly requested in the initial specification, but it's a good test for `GROUP BY`, `HAVING`, and `JOIN`.

## Audit Table — Common Design (to be established together)

`TicketAudit` must contain:

- its own identifier;
- reference to the original ticket;
- relevant old fields (title, description, old status, old priority);
- modification date;
- modification type (`update` or `delete`).

A writes to this table, from the update and delete procedures; B reads from it, through the audit log procedure. This is the **integration point** between the two.
