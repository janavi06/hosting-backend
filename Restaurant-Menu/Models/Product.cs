using System;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;   // for [BindNever]
using Microsoft.AspNetCore.Mvc;                // for [ValidateNever] (alternative)

namespace Restaurant_Menu.Models
{
    public class Product
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal Price { get; set; }
        public string? ProductDescription { get; set; }
        public string? ImagePath { get; set; }

        public int? CategoryID { get; set; }
        public int? SubCategoryID { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsVeg { get; set; }
        public bool IsAvailable { get; set; } = true;

        // ——— NAVIGATION ———

        [JsonIgnore]
        public virtual Category? Category { get; set; }

        [JsonIgnore]
        public virtual SubCategory? SubCategory { get; set; }

        // ← Add BOTH attributes here:
        [JsonIgnore]
        [BindNever]          // skip binding entirely
        // [ValidateNever]   // OR: skip validation if you prefer
        public virtual ICollection<CustomizationOption> CustomizationOptions
        { get; set; } = new List<CustomizationOption>();
    }
}
