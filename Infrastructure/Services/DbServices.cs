using Application.DTOs;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

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
        if (dto.Title.Length > 100)
            throw new ArgumentException("Title must be less than 100 characters.");
        if (string.IsNullOrWhiteSpace(dto.Description))
            throw new ArgumentException("Description is required.");

        var results = await _db.Database.SqlQueryRaw<CreateTicketResponseDto>(
                "EXEC sp_CreateTicket @Title, @Description, @PriorityId",
                new SqlParameter("@Title", dto.Title),
                new SqlParameter("@Description", dto.Description),
                new SqlParameter("@PriorityId", dto.PriorityId))
            .ToListAsync();
        
        var result = results.FirstOrDefault();
        if (result == null)
            throw new Exception("Failed to create ticket");

        return new TicketDto(
            result.Id,
            result.TicketKey,
            dto.Title,
            dto.Description,
            DateTime.UtcNow,
            TicketStatusType.TODO.ToString(),
            dto.PriorityId.ToString());
    }
    
    // public async Task<TicketDto> UpdateAsync(UpdateTicketDto dto) {
    //
    // }

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
            .Where(a => a.TicketKey == ticket.TicketKey)
            .OrderByDescending(a => a.TicketModifiedAt)
            .Select(t => new TicketAuditDto(
                t.Id,
                t.TicketKey,
                t.TicketTitle,
                t.TicketDescription,
                t.TicketModifiedAt,
                t.TicketModificationType,
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
