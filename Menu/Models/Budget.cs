using Restaurant_Menu.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_System.Models
{
    public class Budget
    {
        [Key]
        public int BudgetID { get; set; }

        [Required]
        public int RestaurantID { get; set; }

        [Required]
        public ExpenseCategory Category { get; set; }

        [Required]
        public decimal MonthlyBudget { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public int Month { get; set; }

        public decimal ActualSpent { get; set; }

        [NotMapped]
        public decimal RemainingBudget => MonthlyBudget - ActualSpent;

        [NotMapped]
        public bool IsOverBudget => ActualSpent > MonthlyBudget;

        // Navigation properties
        [ForeignKey("RestaurantID")]
        public Restaurant Restaurant { get; set; }
    }
}