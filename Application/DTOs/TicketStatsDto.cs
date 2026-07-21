namespace Application.DTOs;

public record TicketStatsDto(
    string Status,
    string Priority,
    int TotalTickets);