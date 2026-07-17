using Application.DTOs;
using Infrastructure.Services;

namespace IssueTracker.Endpoints;

public static class TicketEndpoints
{
    public static RouteGroupBuilder MapTicketEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (DbServices dbs, CreateTicketDto dto) =>
        {
            try
            {
                TicketDto ticket = await dbs.CreateAsync(dto);
                return Results.Created($"/api/tickets/{ticket.TicketKey}", ticket);
            }
            catch (ArgumentException e)
            {
                return Results.Problem(
                    title: "Invalid ticket data",
                    detail: e.Message,
                    statusCode: StatusCodes.Status400BadRequest);
            }
        });
        
        group.MapGet("/", async (DbServices dbs) =>
        {
            List<TicketDto> tickets = await dbs.GetAllAsync();

            return Results.Ok(tickets);
        });

        group.MapGet("/{ticketKey}", async (string ticketKey, DbServices dbs) =>
        {
            TicketDto? ticket = await dbs.GetTicketAsync(ticketKey);

            if (ticket == null)
            {
                return Results.Problem(
                    type: "Not Found",
                    title: "Invalid ticket key",
                    detail: "Try again with a valid ticket key",
                    statusCode: StatusCodes.Status404NotFound
                );
            }

            return Results.Ok(ticket);
        });
        
        group.MapDelete("/{ticketKey}", async (string ticketKey, DbServices dbs) =>
        {
            bool deleted = await dbs.DeleteAsync(ticketKey);
            if (!deleted)
            {
                return Results.Problem(
                    type: "Not Found",
                    title: "Invalid ticket key",
                    detail: "Try again with a valid ticket key",
                    statusCode: StatusCodes.Status404NotFound
                );
            }

            return Results.Ok($"Ticket-ul cu cheia {ticketKey} a fost sters cu succes!");
        });

        group.MapGet("/{ticketKey}/audit", async (string ticketKey, DbServices dbs) =>
        {
            TicketDto? ticket = await dbs.GetTicketAsync(ticketKey);
            if (ticket == null)
            {
                return Results.Problem(
                    type: "Not Found",
                    title: "Invalid ticket key",
                    detail: "Try again with a valid ticket key",
                    statusCode: StatusCodes.Status404NotFound
                );
            }

            List<TicketAuditDto> audits = await dbs.GetAuditAsync(ticketKey);
            if (audits.Count == 0)
            {
                return Results.Problem(
                    type: "Not Found",
                    title: "No logs found",
                    detail: $"No audit history exists for ticket {ticketKey}",
                    statusCode: StatusCodes.Status404NotFound
                );
            }

            return Results.Ok(audits);
        });

        return group;
    }
}