using Microsoft.AspNetCore.Identity;

namespace HouseholdBudget.Data;

public class ApplicationUser : IdentityUser
{
    public int? HouseholdId { get; set; }
    public Household? Household { get; set; }

    // Friendly name shown in the UI (the part of the email before the @)
    public string Name => UserName?.Split('@')[0] ?? "Member";
}
