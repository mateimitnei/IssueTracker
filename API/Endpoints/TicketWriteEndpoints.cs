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
        group.MapPatch("/{ticketKey}", async (DbServices dbs, UpdateTicketDto dto) =>
        {
            TicketDto ticket = await dbs.UpdateAsync(dto);
            return Results.Ok(ticket);
        });

        group.MapDelete("/{ticketKey}", async (string ticketKey, DbServices dbs) =>
        {
            await dbs.DeleteAsync(ticketKey);
            return Results.Ok($"Ticket with key {ticketKey} was successfully deleted!");
        });

        return group;
    }
}