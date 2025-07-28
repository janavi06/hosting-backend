namespace Restaurant_Menu.Models
{
    public class Review
    {
        public int ReviewID { get; set; }
        public int OrderID { get; set; }     // Tied to the order in which the product was purchased
        public int Stars { get; set; }       // Star rating (1–5)
        public DateTime CreatedAt { get; set; }  // When the review was made

    }
}
