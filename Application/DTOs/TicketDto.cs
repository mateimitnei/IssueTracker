namespace Application.DTOs;

public sealed record TicketDto(
    int Id,
    string TicketKey,
    string Title,
    string Description,
    DateTime CreatedAt,
    string Status,
    string Priority);