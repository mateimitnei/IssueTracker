using Domain.Entities;

namespace Application.DTOs;

public sealed record UpdateTicketDto(
    string? Title,
    string? Description,
    TicketStatus? StatusId,
    TicketPriorityType? PriorityId);
