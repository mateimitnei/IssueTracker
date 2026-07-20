using Application.DTOs;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class DbServices {
    private readonly AppDbContext _db;

    public DbServices(AppDbContext db) {
        _db = db;
    }
    
    public async Task<TicketDto> CreateAsync(CreateTicketDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("Title is required.");
        
        if (dto.PriorityId is < TicketPriorityType.LOW or > TicketPriorityType.HIGH)
            throw new ArgumentException("Priority must be 1, 2 or 3.");

        var nextKey = await GetNextTicketKeyAsync();

        var ticket = new Ticket
        {
            TicketKey = $"TK-{nextKey}",
            Title = dto.Title,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow,
            StatusId = (byte) TicketStatusType.TODO,
            PriorityId = (byte) dto.PriorityId
        };

        await _db.Tickets.AddAsync(ticket);
        await _db.SaveChangesAsync();

        return new TicketDto(
            ticket.Id,
            ticket.TicketKey,
            ticket.Title,
            ticket.Description,
            ticket.CreatedAt,
            TicketStatusType.TODO.ToString(),
            dto.PriorityId.ToString());
    }

    public async Task<List<TicketDto>> GetAllAsync()
    {
        return await _db.Tickets
            .Select(t => new TicketDto(
                t.Id,
                t.TicketKey,
                t.Title,
                t.Description,
                t.CreatedAt,
                t.Status.Name,
                t.Priority.Name))
            .ToListAsync();
    }

    public async Task<TicketDto?> GetTicketAsync(string ticketKey)
    {
        return await _db.Tickets
            .Where(t => t.TicketKey == ticketKey)
            .Select(t => new TicketDto(
                t.Id,
                t.TicketKey,
                t.Title,
                t.Description,
                t.CreatedAt,
                t.Status.Name,
                t.Priority.Name))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteAsync(string ticketKey)
    {
        var foundTicket = await _db.Tickets.FirstOrDefaultAsync(t => t.TicketKey == ticketKey);

        if (foundTicket == null)
            return false;

        _db.Tickets.Remove(foundTicket);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<TicketAuditDto>> GetAuditAsync(string ticketKey)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.TicketKey == ticketKey);

        if (ticket == null)
            return [];

        return await _db.TicketAudits
            .Where(a => a.TicketId == ticket.Id)
            .OrderByDescending(a => a.TicketModifiedAt)
            .Select(t => new TicketAuditDto(
                t.Id,
                t.TicketId,
                t.TicketTitle,
                t.TicketModifiedAt,
                t.Status.Name,
                t.Priority.Name))
            .ToListAsync();
    }

    private async Task<int> GetNextTicketKeyAsync()
    {
        var ticketKeys = await _db.Tickets
            .AsNoTracking()
            .Select(t => t.TicketKey)
            .ToListAsync();

        var maxValue = ticketKeys
            .Where(key => key.StartsWith("TK-"))
            .Select(key => int.TryParse(key[3..], out var value) ? value : 100)
            .DefaultIfEmpty(100)
            .Max();

        return maxValue + 1;
    }
}
