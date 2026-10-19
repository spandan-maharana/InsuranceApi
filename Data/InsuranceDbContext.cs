using Microsoft.EntityFrameworkCore;
using InsuranceApi.Models;

namespace InsuranceApi.Data;

public class InsuranceDbContext : DbContext
{
    public InsuranceDbContext(DbContextOptions<InsuranceDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductQuestion> ProductQuestions => Set<ProductQuestion>();
    public DbSet<RatingRules> RatingRules => Set<RatingRules>();
    public DbSet<QuoteRequest> QuoteRequests => Set<QuoteRequest>();
    public DbSet<AppUser> Users => Set<AppUser>();

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
            entity.Property(q => q.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(q => q.CoverageAmount).HasPrecision(18, 2);
            entity.Property(q => q.Premium).HasPrecision(18, 2);

            entity.HasOne(q => q.Customer)
                  .WithMany()
                  .HasForeignKey(q => q.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

            // A product that has quotes cannot be deleted
            entity.HasOne(q => q.Product)
                  .WithMany()
                  .HasForeignKey(q => q.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Policy>(entity =>
        {
            entity.Property(p => p.PolicyNumber).HasMaxLength(30);
            entity.HasIndex(p => p.PolicyNumber).IsUnique();

            entity.HasIndex(p => new { p.Segment, p.Term }).IsUnique();

            entity.HasIndex(p => p.QuoteId).IsUnique().HasFilter("[Term] = 0");

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

            // A product that has policies cannot be deleted
            entity.HasOne(p => p.Product)
                  .WithMany()
                  .HasForeignKey(p => p.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.Property(c => c.Code).HasMaxLength(30);
            entity.Property(c => c.Name).HasMaxLength(100);

            // No two categories can share a code
            entity.HasIndex(c => c.Code).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Code).HasMaxLength(30);
            entity.Property(p => p.Name).HasMaxLength(100);
            entity.Property(p => p.Description).HasMaxLength(500);
            entity.Property(p => p.CoverageLabel).HasMaxLength(50);

            entity.Property(p => p.RatePerThousand).HasPrecision(18, 4);
            entity.Property(p => p.MinimumPremium).HasPrecision(18, 2);
            entity.Property(p => p.PolicyFee).HasPrecision(18, 2);
            entity.Property(p => p.MinCoverage).HasPrecision(18, 2);
            entity.Property(p => p.MaxCoverage).HasPrecision(18, 2);

            // No two products can share a code
            entity.HasIndex(p => p.Code).IsUnique();

            // A category that still has products cannot be deleted
            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductQuestion>(entity =>
        {
            entity.Property(q => q.Key).HasMaxLength(50);
            entity.Property(q => q.Label).HasMaxLength(200);
            entity.Property(q => q.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(q => q.Options).HasMaxLength(500);
            entity.Property(q => q.MinValue).HasPrecision(18, 2);
            entity.Property(q => q.MaxValue).HasPrecision(18, 2);

            entity.HasIndex(q => new { q.ProductId, q.Key }).IsUnique();

            entity.HasOne(q => q.Product)
                  .WithMany(p => p.Questions)
                  .HasForeignKey(q => q.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RatingRules>(entity =>
        {
            entity.Property(r => r.MatchValue).HasMaxLength(100);
            entity.Property(r => r.Description).HasMaxLength(200);
            entity.Property(r => r.FromValue).HasPrecision(18, 2);
            entity.Property(r => r.ToValue).HasPrecision(18, 2);
            entity.Property(r => r.Multiplier).HasPrecision(8, 4);

            entity.HasOne(r => r.Question)
                  .WithMany(q => q.Rules)
                  .HasForeignKey(r => r.QuestionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuoteRequest>(entity =>
        {
            entity.Property(r => r.FirstName).HasMaxLength(100);
            entity.Property(r => r.LastName).HasMaxLength(100);
            entity.Property(r => r.Email).HasMaxLength(256);
            entity.Property(r => r.PhoneNumber).HasMaxLength(30);
            entity.Property(r => r.DeclineReason).HasMaxLength(500);

            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(r => r.CoverageAmount).HasPrecision(18, 2);

            entity.HasIndex(r => new { r.Status, r.CreatedAt });

            entity.HasOne(r => r.Product)
                  .WithMany()
                  .HasForeignKey(r => r.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property(u => u.Username).HasMaxLength(50);
            entity.Property(u => u.DisplayName).HasMaxLength(100);
            entity.Property(u => u.PasswordHash).HasMaxLength(200);
            entity.Property(u => u.Role).HasMaxLength(20);

            // No two users can share a username
            entity.HasIndex(u => u.Username).IsUnique();
        });
    }
}