namespace OnlineShopTests;

public class LoggedFactAttribute : FactAttribute
{
    public string Category { get; set; } = "General";
    public int Points { get; set; } = 1;
}