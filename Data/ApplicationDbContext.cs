using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HouseholdBudget.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Household> Households => Set<Household>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Household>()
            .HasIndex(h => h.InviteCode)
            .IsUnique();

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Household)
            .WithMany(h => h.Members)
            .HasForeignKey(u => u.HouseholdId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Category>()
            .HasOne(c => c.Household)
            .WithMany(h => h.Categories)
            .HasForeignKey(c => c.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Category>()
            .HasOne(c => c.Budget)
            .WithOne(b => b.Category)
            .HasForeignKey<Budget>(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Budget>()
            .Property(b => b.MonthlyLimit)
            .HasPrecision(18, 2);

        builder.Entity<Expense>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);

            e.HasOne(x => x.Household)
                .WithMany(h => h.Expenses)
                .HasForeignKey(x => x.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Category)
                .WithMany(c => c.Expenses)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.PaidBy)
                .WithMany()
                .HasForeignKey(x => x.PaidByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
