namespace SneCafe.Models
{
    public record SizeOption(string Name, decimal Extra);
    public record ToppingOption(string Name, decimal Price);

    public static class MenuOptions
    {
        public const decimal TaxRate = 0.08m;

        public static readonly List<SizeOption> Sizes = new()
        {
            new SizeOption("Small",  0m),
            new SizeOption("Medium", 0.50m),
            new SizeOption("Large",  1.00m)
        };

        public static readonly string[] SugarLevels = { "0%", "25%", "50%", "75%", "100%" };

        public static readonly List<ToppingOption> Toppings = new()
        {
            new ToppingOption("Extra shot",    0.80m),
            new ToppingOption("Whipped cream", 0.50m),
            new ToppingOption("Caramel syrup", 0.50m),
            new ToppingOption("Pearls",        0.70m)
        };
    }
}
