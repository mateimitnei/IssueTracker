using Domain.Entities;

namespace Application.DTOs;

public sealed record UpdateTicketDto(
    string TicketKey,
    string? Title = null,
    string? Description = null,
    TicketStatusType? StatusId = null,
    TicketPriorityType? PriorityId = null);
