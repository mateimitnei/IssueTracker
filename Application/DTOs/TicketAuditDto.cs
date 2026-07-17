namespace Application.DTOs;

public sealed record TicketAuditDto(
    int Id,
    int TicketId,
    string TicketTitle,
    DateTime TicketModifiedAt,
    string Status,
    string Priority);