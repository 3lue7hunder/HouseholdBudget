using System.ComponentModel.DataAnnotations;

namespace HouseholdBudget.Data;

/// <summary>One purchase or bill. It belongs to a household and a category, and records who paid.</summary>
public class Expense
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(120)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 100000000, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    [StringLength(300)]
    public string? Notes { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a category.")]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int HouseholdId { get; set; }
    public Household? Household { get; set; }

    public string? PaidByUserId { get; set; }
    public ApplicationUser? PaidBy { get; set; }
}
