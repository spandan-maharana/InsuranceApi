using Microsoft.EntityFrameworkCore;
using InsuranceApi.Models;

namespace InsuranceApi.Data;

public class InsuranceDbContext : DbContext
{
    public InsuranceDbContext(DbContextOptions<InsuranceDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) //This is a model validation step for the database, where the rule is 'no two customers can have the same email address'.
    {
        // Configure the Customer entity
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(c => c.Email).HasMaxLength(256);

            // The database itself will also refuse duplicate emails
            entity.HasIndex(c => c.Email).IsUnique();
        });
    }
}