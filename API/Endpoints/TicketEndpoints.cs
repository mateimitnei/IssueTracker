using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Endpoints;

public static class TicketEndpoints
{
    public static RouteGroupBuilder MapTicketEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async(AppDbContext db) =>
        {
            // INNER JOIN with TicketStatus and TicketPriority
            var tickets = await db.Tickets.Include(t => t.Status)
                .Include(t => t.Priority)
                .ToListAsync();

            return tickets;
        });

        group.MapGet("/{ticketKey}", async (string ticketKey, AppDbContext db) =>
        {
            var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.TicketKey == ticketKey);

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

        group.MapDelete("/{ticketKey}", async (string ticketKey, AppDbContext db) =>
        {
            var foundTicket = await db.Tickets.FirstOrDefaultAsync(t => t.TicketKey == ticketKey);

            if (foundTicket == null)
            {
                return Results.Problem(
                    type: "Not Found",
                    title: "Invalid ticket key",
                    detail: "Try again with a valid ticket key",
                    statusCode: StatusCodes.Status404NotFound
                );
            }

            db.Remove(foundTicket);
            await db.SaveChangesAsync();

            return Results.Ok($"Ticket-ul cu cheia {ticketKey} a fost sters cu succes!");
        });

        group.MapGet("/{ticketKey}/audit", async (string ticketKey, AppDbContext db) =>
        {
            var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.TicketKey == ticketKey);

            if (ticket == null)
            {
                return Results.Problem(
                    type: "Not Found",
                    title: "Invalid ticket key",
                    detail: "Try again with a valid ticket key",
                    statusCode: StatusCodes.Status404NotFound
                );
            }

            var audits = await db.TicketAudits
                .Include(a => a.Status)
                .Include(a => a.Priority)
                .Where(a => a.TicketId == ticket.Id)
                .OrderByDescending(a => a.TicketModifiedAt)
                .ToListAsync();

            if (!audits.Any())
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