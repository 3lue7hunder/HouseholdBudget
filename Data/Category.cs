using System.ComponentModel.DataAnnotations;

namespace HouseholdBudget.Data;

/// <summary>A spending category (such as Groceries) that belongs to one household.</summary>
public class Category
{
    public int Id { get; set; }

    [Required, StringLength(60)]
    public string Name { get; set; } = string.Empty;

    public int HouseholdId { get; set; }
    public Household? Household { get; set; }

    public Budget? Budget { get; set; }
    public List<Expense> Expenses { get; set; } = new();
}
