namespace Domain.Entities;

public class TicketAudit
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string TicketTitle { get; set; } = string.Empty;
    public DateTime TicketModifiedAt { get; set; }

    // FK for TicketStatus
    public byte TicketStatusId { get; set; }
    public TicketStatus Status { get; set; } = null!;
    
    // FK for TicketPriority
    public byte TicketPriorityId { get; set; }
    public TicketPriority Priority { get; set; } = null!;
}