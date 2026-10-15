using System.Security.Claims;
using HouseholdBudget.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HouseholdBudget.Services;

/// <summary>
/// One category's budget for a month: the limit (if any) and how much has been spent.
/// </summary>
public record BudgetStatus(int CategoryId, string CategoryName, decimal? Limit, decimal Spent)
{
    /// <summary>Money left under the limit. Negative when over budget. Null when there is no limit.</summary>
    public decimal? Remaining => Limit - Spent;

    /// <summary>Percent of the limit that has been spent. Zero when there is no limit.</summary>
    public double Percent => Limit is > 0 ? (double)(Spent / Limit.Value * 100m) : 0;
}

/// <summary>
/// All household, category, expense and budget logic for the signed-in user.
/// Every query is limited to the user's own household, so one household can never
/// read or change another household's data. Problems the user can fix (an empty name,
/// a duplicate category, and so on) are thrown as InvalidOperationException with a
/// readable message that the pages show to the user.
/// </summary>
public class BudgetService(IDbContextFactory<ApplicationDbContext> factory, AuthenticationStateProvider auth)
{
    // Categories every new household starts with.
    private static readonly string[] DefaultCategories =
    [
        "Housing", "Groceries", "Utilities", "Transportation",
        "Dining Out", "Health", "Entertainment", "Savings", "Other"
    ];

    // ---------- Current user and household ----------

    /// <summary>Gets the id of the signed-in user, or null when nobody is signed in.</summary>
    private async Task<string?> UserIdAsync()
    {
        var state = await auth.GetAuthenticationStateAsync();
        return state.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    /// <summary>Gets the signed-in user's household id. Throws if they are not in a household.</summary>
    private async Task<int> HouseholdIdAsync(ApplicationDbContext db)
    {
        var userId = await UserIdAsync() ?? throw new InvalidOperationException("You are not signed in.");
        var householdId = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.HouseholdId)
            .FirstOrDefaultAsync();
        return householdId ?? throw new InvalidOperationException("Create or join a household first.");
    }

    /// <summary>Loads the signed-in user with their household and its members. Null if signed out.</summary>
    public async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = await UserIdAsync();
        if (userId is null) return null;

        await using var db = await factory.CreateDbContextAsync();

        // Identity resolution is needed because Household -> Members points back to the household.
        return await db.Users
            .Include(u => u.Household)
                .ThenInclude(h => h!.Members)
            .AsNoTrackingWithIdentityResolution()
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    /// <summary>Creates a household with an invite code and default categories, and puts the user in it.</summary>
    public async Task<Household> CreateHouseholdAsync(string name)
    {
        var cleaned = (name ?? "").Trim();
        if (cleaned.Length == 0) throw new InvalidOperationException("Please enter a household name.");
        if (cleaned.Length > 80) throw new InvalidOperationException("The household name must be 80 characters or fewer.");

        var userId = await UserIdAsync() ?? throw new InvalidOperationException("You are not signed in.");
        await using var db = await factory.CreateDbContextAsync();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("Your account could not be found. Please log out and log in again.");
        if (user.HouseholdId is not null)
            throw new InvalidOperationException("You are already in a household. Leave it first to create a new one.");

        var household = new Household
        {
            Name = cleaned,
            InviteCode = await NewInviteCodeAsync(db)
        };
        foreach (var categoryName in DefaultCategories)
            household.Categories.Add(new Category { Name = categoryName });

        db.Households.Add(household);
        user.Household = household;
        await db.SaveChangesAsync();
        return household;
    }

    /// <summary>Joins the household with the given invite code. Returns false when no household has that code.</summary>
    public async Task<bool> JoinHouseholdAsync(string code)
    {
        var normalized = (code ?? "").Trim().ToUpperInvariant();
        if (normalized.Length == 0) throw new InvalidOperationException("Please enter an invite code.");

        var userId = await UserIdAsync() ?? throw new InvalidOperationException("You are not signed in.");
        await using var db = await factory.CreateDbContextAsync();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("Your account could not be found. Please log out and log in again.");
        if (user.HouseholdId is not null)
            throw new InvalidOperationException("You are already in a household. Leave it first to join another.");

        var household = await db.Households.FirstOrDefaultAsync(h => h.InviteCode == normalized);
        if (household is null) return false;

        user.HouseholdId = household.Id;
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>Removes the signed-in user from their household. Past expenses stay with the household.</summary>
    public async Task LeaveHouseholdAsync()
    {
        var userId = await UserIdAsync() ?? throw new InvalidOperationException("You are not signed in.");
        await using var db = await factory.CreateDbContextAsync();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("Your account could not be found. Please log out and log in again.");
        user.HouseholdId = null;
        await db.SaveChangesAsync();
    }

    /// <summary>Makes an 8 character invite code that no other household uses.</summary>
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

    /// <summary>Gets the household's categories in alphabetical order.</summary>
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

    /// <summary>Checks a category name and returns it trimmed. Throws a readable error if it is not allowed.</summary>
    private static string ValidateCategoryName(string name)
    {
        var cleaned = (name ?? "").Trim();
        if (cleaned.Length == 0) throw new InvalidOperationException("Please enter a category name.");
        if (cleaned.Length > 60) throw new InvalidOperationException("The category name must be 60 characters or fewer.");
        return cleaned;
    }

    /// <summary>Adds a category to the household. Names must be unique, ignoring upper and lower case.</summary>
    public async Task AddCategoryAsync(string name)
    {
        var cleaned = ValidateCategoryName(name);
        await using var db = await factory.CreateDbContextAsync();
        var householdId = await HouseholdIdAsync(db);

        var exists = await db.Categories.AnyAsync(c =>
            c.HouseholdId == householdId && c.Name.ToLower() == cleaned.ToLower());
        if (exists) throw new InvalidOperationException($"A category named \"{cleaned}\" already exists.");

        db.Categories.Add(new Category { Name = cleaned, HouseholdId = householdId });
        await db.SaveChangesAsync();
    }

    /// <summary>Renames a category. The new name must not match another category in the household.</summary>
    public async Task RenameCategoryAsync(int id, string name)
    {
        var cleaned = ValidateCategoryName(name);
        await using var db = await factory.CreateDbContextAsync();
        var householdId = await HouseholdIdAsync(db);

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.HouseholdId == householdId)
            ?? throw new InvalidOperationException("That category no longer exists.");

        var exists = await db.Categories.AnyAsync(c =>
            c.HouseholdId == householdId && c.Id != id && c.Name.ToLower() == cleaned.ToLower());
        if (exists) throw new InvalidOperationException($"A category named \"{cleaned}\" already exists.");

        category.Name = cleaned;
        await db.SaveChangesAsync();
    }

    /// <summary>Deletes a category. Returns false when it still has expenses, so none are left without a category.</summary>
    public async Task<bool> DeleteCategoryAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var householdId = await HouseholdIdAsync(db);
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.HouseholdId == householdId)
            ?? throw new InvalidOperationException("That category no longer exists.");

        if (await db.Expenses.AnyAsync(e => e.CategoryId == id))
            return false;

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return true;
    }

    // ---------- Expenses ----------

    /// <summary>Gets the household's expenses for one month, newest first.</summary>
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

    /// <summary>
    /// Adds an expense (Id is 0) or updates one (Id is set). New expenses record the signed-in
    /// user as the payer. Editing keeps the original payer.
    /// </summary>
    public async Task SaveExpenseAsync(Expense input)
    {
        var description = (input.Description ?? "").Trim();
        if (description.Length == 0) throw new InvalidOperationException("Please enter a description.");
        if (description.Length > 120) throw new InvalidOperationException("The description must be 120 characters or fewer.");
        if (input.Amount <= 0) throw new InvalidOperationException("The amount must be greater than 0.");

        var userId = await UserIdAsync() ?? throw new InvalidOperationException("You are not signed in.");
        await using var db = await factory.CreateDbContextAsync();
        var householdId = await HouseholdIdAsync(db);

        // The category must belong to this household.
        var categoryOk = await db.Categories.AnyAsync(c => c.Id == input.CategoryId && c.HouseholdId == householdId);
        if (!categoryOk) throw new InvalidOperationException("Please choose a valid category.");

        var notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();

        if (input.Id == 0)
        {
            db.Expenses.Add(new Expense
            {
                Description = description,
                Amount = Math.Round(input.Amount, 2),
                Date = input.Date.Date,
                Notes = notes,
                CategoryId = input.CategoryId,
                HouseholdId = householdId,
                PaidByUserId = userId
            });
        }
        else
        {
            var existing = await db.Expenses.FirstOrDefaultAsync(e => e.Id == input.Id && e.HouseholdId == householdId)
                ?? throw new InvalidOperationException("That expense no longer exists.");
            existing.Description = description;
            existing.Amount = Math.Round(input.Amount, 2);
            existing.Date = input.Date.Date;
            existing.Notes = notes;
            existing.CategoryId = input.CategoryId;
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Deletes an expense from the household.</summary>
    public async Task DeleteExpenseAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var householdId = await HouseholdIdAsync(db);
        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.HouseholdId == householdId)
            ?? throw new InvalidOperationException("That expense no longer exists.");
        db.Expenses.Remove(expense);
        await db.SaveChangesAsync();
    }

    // ---------- Budgets ----------

    /// <summary>Gets every category with its monthly limit (if set) and what was spent in the given month.</summary>
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

    /// <summary>Sets a category's monthly limit. A null or zero limit removes the budget.</summary>
    public async Task SetBudgetAsync(int categoryId, decimal? limit)
    {
        if (limit is > 100000000m)
            throw new InvalidOperationException("That limit is too large.");

        await using var db = await factory.CreateDbContextAsync();
        var householdId = await HouseholdIdAsync(db);

        var categoryOk = await db.Categories.AnyAsync(c => c.Id == categoryId && c.HouseholdId == householdId);
        if (!categoryOk) throw new InvalidOperationException("That category no longer exists.");

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
