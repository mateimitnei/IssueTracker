using Application.DTOs;
using Infrastructure.Services;

namespace IssueTracker.Endpoints;

public static class TicketWriteEndpoints
{
    public static RouteGroupBuilder MapTicketWriteEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (DbServices dbs, CreateTicketDto dto) =>
        {
            TicketDto ticket = await dbs.CreateAsync(dto);
            return Results.Created($"/api/tickets/{ticket.TicketKey}", ticket);
        });
        
        // TODO: PATCH

        group.MapDelete("/{ticketKey}", async (string ticketKey, DbServices dbs) =>
        {
            bool deleted = await dbs.DeleteAsync(ticketKey);
            if (!deleted) {
                throw new KeyNotFoundException($"Ticket with key {ticketKey} does not exist!");
            }
            return Results.Ok($"Ticket with key {ticketKey} was successfully deleted!");
        });

        return group;
    }
}