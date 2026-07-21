namespace Application.DTOs;

public sealed record TicketAuditDto(
    int Id,
    int TicketId,
    string TicketKey,
    string TicketTitle,
    string? TicketDescription,
    DateTime TicketModifiedAt,
    string TicketModificationType,
    string Status,
    string Priority);