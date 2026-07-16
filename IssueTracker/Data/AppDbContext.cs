using Microsoft.EntityFrameworkCore;
using IssueTracker.Models;

namespace IssueTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

    public DbSet<TicketStatus> TicketStatuses { get; set; }
    public DbSet<TicketPriority> TicketPriorities { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<TicketAudit> TicketAudits { get; set; }
}