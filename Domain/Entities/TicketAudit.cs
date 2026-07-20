namespace Domain.Entities;

public class TicketAudit
{
    public int Id { get; set; }
    public string TicketKey { get; set; }
    public string TicketTitle { get; set; } = string.Empty;
    public string? TicketDescription { get; set; }
    public DateTime TicketModifiedAt { get; set; }
    public string TicketModificationType { get; set; } = string.Empty;

    // FK for TicketStatus
    public byte TicketStatusId { get; set; }
    public TicketStatus Status { get; set; } = null!;
    
    // FK for TicketPriority
    public byte TicketPriorityId { get; set; }
    public TicketPriority Priority { get; set; } = null!;
}