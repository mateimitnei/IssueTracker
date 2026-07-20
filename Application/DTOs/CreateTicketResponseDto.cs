using Domain.Entities;

namespace Application.DTOs;

public sealed record CreateTicketResponseDto(
    int Id,
    string TicketKey
);
