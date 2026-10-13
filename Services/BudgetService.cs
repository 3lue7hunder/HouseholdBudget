using System.Security.Claims;
using HouseholdBudget.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HouseholdBudget.Services;

public record BudgetStatus(int CategoryId, string CategoryName, decimal? Limit, decimal Spent)
{
    public decimal? Remaining => Limit - Spent;
    public double Percent => Limit is > 0 ? (double)(Spent / Limit.Value * 100m) : 0;
}

public class BudgetService(IDbContextFactory<ApplicationDbContext> factory, AuthenticationStateProvider auth)
{
    private static readonly string[] DefaultCategories =
    [
        "Housing", "Groceries", "Utilities", "Transportation",
        "Dining Out", "Health", "Entertainment", "Savings", "Other"
    ];

    // ---------- Current user and household ----------

    private async Task<string?> UserIdAsync()
    {
        var state = await auth.GetAuthenticationStateAsync();
        return state.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private async Task<int> HouseholdIdAsync(ApplicationDbContext db)
    {
        var userId = await UserIdAsync() ?? throw new InvalidOperationException("You are not signed in.");
        var householdId = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.HouseholdId)
            .FirstOrDefaultAsync();
        return householdId ?? throw new InvalidOperationException("Create or join a household first.");
    }

    public async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = await UserIdAsync();
        if (userId is null) return null;

        await using var db = await factory.CreateDbContextAsync();
        return await db.Users
            .Include(u => u.Household)
                .ThenInclude(h => h!.Members)
            .AsNoTrackingWithIdentityResolution()
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<Household> CreateHouseholdAsync(string name)
    {
        var userId = await UserIdAsync() ?? throw new InvalidOperationException("You are not signed in.");
        await using var db = await factory.CreateDbContextAsync();

        var user = await db.Users.FirstAsync(u => u.Id == userId);
        var household = new Household
        {
            Name = name.Trim(),
            InviteCode = await NewInviteCodeAsync(db)
        };
        foreach (var categoryName in DefaultCategories)
            household.Categories.Add(new Category { Name = categoryName });

        db.Households.Add(household);
        user.Household = household;
        await db.SaveChangesAsync();
        return household;
    }

    public async Task<bool> JoinHouseholdAsync(string code)
    {
        var userId = await UserIdAsync() ?? throw new InvalidOperationException("You are not signed in.");
        await using var db = await factory.CreateDbContextAsync();

        var normalized = code.Trim().ToUpperInvariant();
        var household = await db.Households.FirstOrDefaultAsync(h => h.InviteCode == normalized);
        if (household is null) return false;

        var user = await db.Users.FirstAsync(u => u.Id == userId);
        user.HouseholdId = household.Id;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task LeaveHouseholdAsync()
    {
        var userId = await UserIdAsync() ?? throw new InvalidOperationException("You are not signed in.");
        await using var db = await factory.CreateDbContextAsync();

        var user = await db.Users.FirstAsync(u => u.Id == userId);
        user.HouseholdId = null;
        await db.SaveChangesAsync();
    }

    private static async Task<string> NewInviteCodeAsync(ApplicationDbContext db)
    {
        while (true)
        {
            var code = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            if (!await db.Households.AnyAsync(h => h.InviteCode == code))
                return code;
        }
    }

    // ---------- Categories ----------

    public async Task<List<Category>> GetCategoriesAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        var householdId = await HouseholdIdAsync(db);
        return await db.Categories
            .Where(c => c.HouseholdId == householdId)
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task AddCategoryAsync(string name)
    {
        await using var db = await factory.CreateDbContextAsync();
        var householdId = await HouseholdIdAsync(db);
        db.Categories.Add(new Category { Name = name.Trim(), HouseholdId = householdId });
        await db.SaveChangesAsync();
    }

    public async Task RenameCategoryAsync(int id, string name)
    {
        await using var db = await factory.CreateDbContextAsync();
        var householdId = await HouseholdIdAsync(db);
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.HouseholdId == householdId)
            ?? throw new InvalidOperationException("Category not found.");
        category.Name = name.Trim();
        await db.SaveChangesAsync();
    }

    // Returns false when the category still has expenses and cannot be removed.
    public async Task<bool> DeleteCategoryAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var householdId = await HouseholdIdAsync(db);
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.HouseholdId == householdId)
            ?? throw new InvalidOperationException("Category not found.");

        if (await db.Expenses.AnyAsync(e => e.CategoryId == id))
            return false;

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return true;
    }

    // ---------- Expenses ----------

    public async Task<List<Expense>> GetExpensesAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        await using var db = await factory.CreateDbContextAsync();
        var householdId = await HouseholdIdAsync(db);
        return await db.Expenses
            .Where(e => e.HouseholdId == householdId && e.Date >= start && e.Date < end)
            .Include(e => e.Category)
            .Include(e => e.PaidBy)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task SaveExpenseAsync(Expense input)
    {
        var userId = await UserIdAsync() ?? throw new InvalidOperationException("You are not signed in.");
        await using var db = await factory.CreateDbContextAsync();
        var householdId = await HouseholdIdAsync(db);

        var categoryOk = await db.Categories.AnyAsync(c => c.Id == input.CategoryId && c.HouseholdId == householdId);
        if (!categoryOk) throw new InvalidOperationException("Please choose a valid category.");

        if (input.Id == 0)
        {
            db.Expenses.Add(new Expense
            {
                Description = input.Description.Trim(),
                Amount = Math.Round(input.Amount, 2),
                Date = input.Date.Date,
                Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
                CategoryId = input.CategoryId,
                HouseholdId = householdId,
                PaidByUserId = userId
            });
        }
        else
        {
            var existing = await db.Expenses.FirstOrDefaultAsync(e => e.Id == input.Id && e.HouseholdId == householdId)
                ?? throw new InvalidOperationException("Expense not found.");
            existing.Description = input.Description.Trim();
            existing.Amount = Math.Round(input.Amount, 2);
            existing.Date = input.Date.Date;
            existing.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
            existing.CategoryId = input.CategoryId;
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteExpenseAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var householdId = await HouseholdIdAsync(db);
        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.HouseholdId == householdId)
            ?? throw new InvalidOperationException("Expense not found.");
        db.Expenses.Remove(expense);
        await db.SaveChangesAsync();
    }

    // ---------- Budgets ----------

    public async Task<List<BudgetStatus>> GetBudgetStatusAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        await using var db = await factory.CreateDbContextAsync();
        var householdId = await HouseholdIdAsync(db);

        var categories = await db.Categories
            .Where(c => c.HouseholdId == householdId)
            .Include(c => c.Budget)
            .AsNoTracking()
            .ToListAsync();

        // SQLite cannot SUM decimals in SQL, so totals are calculated in memory.
        var spending = await db.Expenses
            .Where(e => e.HouseholdId == householdId && e.Date >= start && e.Date < end)
            .Select(e => new { e.CategoryId, e.Amount })
            .ToListAsync();

        var spentByCategory = spending
            .GroupBy(x => x.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        return categories
            .OrderBy(c => c.Name)
            .Select(c => new BudgetStatus(
                c.Id,
                c.Name,
                c.Budget?.MonthlyLimit,
                spentByCategory.GetValueOrDefault(c.Id)))
            .ToList();
    }

    // A null or zero limit removes the budget for that category.
    public async Task SetBudgetAsync(int categoryId, decimal? limit)
    {
        await using var db = await factory.CreateDbContextAsync();
        var householdId = await HouseholdIdAsync(db);

        var categoryOk = await db.Categories.AnyAsync(c => c.Id == categoryId && c.HouseholdId == householdId);
        if (!categoryOk) throw new InvalidOperationException("Category not found.");

        var existing = await db.Budgets.FirstOrDefaultAsync(b => b.CategoryId == categoryId);

        if (limit is null or <= 0)
        {
            if (existing is not null) db.Budgets.Remove(existing);
        }
        else if (existing is null)
        {
            db.Budgets.Add(new Budget { CategoryId = categoryId, MonthlyLimit = Math.Round(limit.Value, 2) });
        }
        else
        {
            existing.MonthlyLimit = Math.Round(limit.Value, 2);
        }

        await db.SaveChangesAsync();
    }
}
