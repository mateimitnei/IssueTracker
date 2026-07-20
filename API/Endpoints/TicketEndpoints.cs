using Application.DTOs;
using Infrastructure.Services;

namespace IssueTracker.Endpoints;

public static class TicketEndpoints
{
    public static RouteGroupBuilder MapTicketEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (DbServices dbs, CreateTicketDto dto) =>
        {
            // GlobalExceptionHandler catches the error dbs.CreateAsync throws
            TicketDto ticket = await dbs.CreateAsync(dto);
            return Results.Created($"/api/tickets/{ticket.TicketKey}", ticket);
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
                throw new KeyNotFoundException($"Try again with a valid ticket key: {ticketKey}");
            }

            return Results.Ok(ticket);
        });
        
        group.MapDelete("/{ticketKey}", async (string ticketKey, DbServices dbs) =>
        {
            bool deleted = await dbs.DeleteAsync(ticketKey);
            
            if (!deleted)
            {
                throw new KeyNotFoundException($"Try again with a valid ticket key: {ticketKey}");
            }

            return Results.Ok($"Ticket with key {ticketKey} was successfully deleted!");
        });

        group.MapGet("/{ticketKey}/audit", async (string ticketKey, DbServices dbs) =>
        {
            TicketDto? ticket = await dbs.GetTicketAsync(ticketKey);
            
            if (ticket == null)
            {
                throw new KeyNotFoundException($"Try again with a valid ticket key: {ticketKey}");
            }

            List<TicketAuditDto> audits = await dbs.GetAuditAsync(ticketKey);
            
            if (audits.Count == 0)
            {
                throw new KeyNotFoundException($"No audit history exists for ticket {ticketKey}");
            }

            return Results.Ok(audits);
        });

        return group;
    }
}