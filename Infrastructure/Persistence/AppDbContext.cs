using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

    public DbSet<TicketStatus> TicketStatuses { get; set; }
    public DbSet<TicketPriority> TicketPriorities { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<TicketAudit> TicketAudits { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Ticket>().ToTable("Ticket");
        modelBuilder.Entity<TicketStatus>().ToTable("TicketStatus");
        modelBuilder.Entity<TicketPriority>().ToTable("TicketPriority");
        modelBuilder.Entity<TicketAudit>().ToTable("TicketAudit");
    }
}