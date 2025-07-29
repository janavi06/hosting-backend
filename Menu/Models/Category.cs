using Restaurant_Menu.Models;

public class Category
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public int RestaurantID { get; set; }
    public Restaurant? Restaurant { get; set; }

    public ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
