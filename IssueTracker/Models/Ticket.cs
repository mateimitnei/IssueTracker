namespace IssueTracker.Models;

public class Ticket
{
    public int Id { get; set; }
    public string TicketKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // FK for TicketStatus
    public byte StatusId { get; set; }
    public TicketStatus Status { get; set; } = null!;
    
    // FK for TicketPriority
    public byte PriorityId { get; set; }
    public TicketPriority Priority { get; set; } = null!;
}