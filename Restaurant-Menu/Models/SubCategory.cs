using System.Text.Json.Serialization;

namespace Restaurant_Menu.Models
{
    public class SubCategory
    {
        public int SubCategoryID { get; set; }
        public string SubCategoryName { get; set; }

        public int? CategoryID { get; set; }

        [JsonIgnore]
        public Category? Category { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Product> Products { get; set; } = new List<Product>();

        public SubCategory(string subCategoryName)
        {
            SubCategoryName = subCategoryName;
        }
    }
}
