using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SneCafe.Models
{
    public class CategoryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    // One row in the Menu list (includes hidden items)
    public class MenuRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public string Category { get; set; } = "";
        public string Icon { get; set; } = "bi-cup-hot";
        public string? ImageUrl { get; set; }
        public bool HasOptions { get; set; }
        public bool IsAvailable { get; set; }
        public int TimesOrdered { get; set; }      // how many order lines use this item
    }

    // The icons an admin can pick for an item
    public class IconOption
    {
        public string Value { get; set; } = "";
        public string Label { get; set; } = "";
    }

    public static class MenuIcons
    {
        public static readonly List<IconOption> All = new()
        {
            new IconOption { Value = "bi-cup-hot",     Label = "Hot cup (coffee)" },
            new IconOption { Value = "bi-cup",         Label = "Cup (tea)" },
            new IconOption { Value = "bi-cup-straw",   Label = "Cup with straw (juice, smoothie)" },
            new IconOption { Value = "bi-snow",        Label = "Snowflake (iced)" },
            new IconOption { Value = "bi-droplet",     Label = "Water drop" },
            new IconOption { Value = "bi-cake2",       Label = "Cake slice" },
            new IconOption { Value = "bi-cake",        Label = "Cake" },
            new IconOption { Value = "bi-circle-fill", Label = "Round (cookie)" },
            new IconOption { Value = "bi-egg-fried",   Label = "Egg (bagel, breakfast)" },
            new IconOption { Value = "bi-basket",      Label = "Basket (sandwich)" },
            new IconOption { Value = "bi-flower1",     Label = "Leaf (salad)" }
        };

        public static bool IsValid(string? value) => All.Any(i => i.Value == value);
    }

    // The Add / Edit form
    public class ProductEditModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Enter the item name")]
        [StringLength(100)]
        [RegularExpression("^[^<>\"&`]+$", ErrorMessage = "The name cannot contain < > \" & or a backtick")]
        public string Name { get; set; } = "";

        [Range(1, int.MaxValue, ErrorMessage = "Choose a category")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Range(0.01, 999.99, ErrorMessage = "Price must be between 0.01 and 999.99")]
        public decimal Price { get; set; }

        public string Icon { get; set; } = "bi-cup-hot";

        [StringLength(255)]
        [RegularExpression(@"^(/[A-Za-z0-9._~%/-]+|https?://[^\s""'<>`]+)?$",
            ErrorMessage = "Use a link starting with https:// (or a path like /images/latte.jpg)")]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        // A picture uploaded from the computer
        public IFormFile? ImageFile { get; set; }

        // Tick to take the picture away
        [Display(Name = "Remove the current picture")]
        public bool RemoveImage { get; set; }

        [Display(Name = "Has size, sugar and topping options")]
        public bool HasOptions { get; set; }

        [Display(Name = "Show on the order screen")]
        public bool IsAvailable { get; set; } = true;
    }

    public class ProductFormViewModel
    {
        public ProductEditModel Form { get; set; } = new();
        public List<CategoryItem> Categories { get; set; } = new();
        public bool IsEdit => Form.Id > 0;
    }
}