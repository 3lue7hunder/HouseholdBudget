using System.ComponentModel.DataAnnotations;

namespace HouseholdBudget.Data;

/// <summary>A group of people who share one budget. Members join by typing the invite code.</summary>
public class Household
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(12)]
    public string InviteCode { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public List<ApplicationUser> Members { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Expense> Expenses { get; set; } = new();
}
