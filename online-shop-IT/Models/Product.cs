using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace online_shop_IT.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
    public int StoreId { get; set; }
    
}