using Restaurant_Menu.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_System.Models
{
    public class CompetitiveAnalysis
    {
        [Key]
        public int AnalysisID { get; set; }

        [Required]
        public int RestaurantID { get; set; }

        [Required]
        public DateTime AnalysisDate { get; set; }

        public string CompetitorName { get; set; }
        public decimal CompetitorAvgPrice { get; set; }
        public decimal CompetitorRating { get; set; }
        public string CompetitorStrengths { get; set; }
        public string CompetitorWeaknesses { get; set; }

        public decimal MarketShare { get; set; }
        public decimal PriceCompetitiveness { get; set; }

        public string Recommendations { get; set; }

        // Navigation properties
        [ForeignKey("RestaurantID")]
        public Restaurant Restaurant { get; set; }
    }
}