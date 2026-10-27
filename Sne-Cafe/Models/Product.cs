namespace SneCafe.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public string Category { get; set; } = "";
        public string Icon { get; set; } = "bi-cup-hot";
        public string? ImageUrl { get; set; }
        public bool HasOptions { get; set; }
    }
}