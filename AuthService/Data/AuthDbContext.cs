using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            // Map to the same table name as the monolith
            entity.ToTable("User");

            entity.HasKey(e => e.UserId).HasName("PK__User__1788CCAC8A5CE041");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Password).HasMaxLength(255).HasDefaultValue("");
        });
    }
}
