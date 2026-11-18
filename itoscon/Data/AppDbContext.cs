using itoscon.Models;
using Microsoft.EntityFrameworkCore;

namespace itoscon.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contact>().HasIndex(c => c.Email).IsUnique();
    }
}
