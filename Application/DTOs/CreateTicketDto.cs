using Domain.Entities;

namespace Application.DTOs;

public sealed record CreateTicketDto(
    string Title,
    string Description,
    TicketPriorityType PriorityId);
