using Microsoft.EntityFrameworkCore;
using InsuranceApi.Models;

namespace InsuranceApi.Data;

public class InsuranceDbContext : DbContext
{
    public InsuranceDbContext(DbContextOptions<InsuranceDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<Policy> Policies => Set<Policy>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) //This is a model validation step for the database, where the rule is 'no two customers can have the same email address'.
    {
        // Configure the Customer entity
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(c => c.Email).HasMaxLength(256);

            // The database itself will also refuse duplicate emails
            entity.HasIndex(c => c.Email).IsUnique();
        });

        modelBuilder.Entity<Quote>(entity =>
        {
            entity.Property(q => q.ProductType).HasConversion<string>().HasMaxLength(20);
            entity.Property(q => q.RiskLevel).HasConversion<string>().HasMaxLength(20);
            entity.Property(q => q.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(q => q.CoverageAmount).HasPrecision(18,2);
            entity.Property(q => q.Premium).HasPrecision(18,2);

            entity.HasOne(q => q.Customer)
                  .WithMany()
                  .HasForeignKey(q => q.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Policy>(entity =>
        {
            entity.Property(p => p.PolicyNumber).HasMaxLength(30);
            entity.HasIndex(p => p.PolicyNumber).IsUnique();

            entity.Property(p => p.ProductType).HasConversion<string>().HasMaxLength(20);
            entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

            entity.Property(p => p.CoverageAmount).HasPrecision(18, 2);
            entity.Property(p => p.Premium).HasPrecision(18, 2);

            // A customer with policies cannot be deleted
            entity.HasOne(p => p.Customer)
                  .WithMany()
                  .HasForeignKey(p => p.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Quote)
                  .WithMany()
                  .HasForeignKey(p => p.QuoteId)
                  .OnDelete(DeleteBehavior.Restrict);

        });
    }
}