using System.ComponentModel.DataAnnotations;

namespace HouseholdBudget.Data;

// One monthly spending limit per category. The limit repeats every month.
/// <summary>A monthly spending limit for one category.</summary>
public class Budget
{
    public int Id { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [Range(0.01, 100000000)]
    public decimal MonthlyLimit { get; set; }
}
