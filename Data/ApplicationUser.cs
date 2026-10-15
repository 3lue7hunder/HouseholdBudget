using Microsoft.AspNetCore.Identity;

namespace HouseholdBudget.Data;

/// <summary>A person who can log in. Extends the Identity user with the household they belong to.</summary>
public class ApplicationUser : IdentityUser
{
    public int? HouseholdId { get; set; }
    public Household? Household { get; set; }

    // Friendly name shown in the UI (the part of the email before the @)
    public string Name => UserName?.Split('@')[0] ?? "Member";
}
