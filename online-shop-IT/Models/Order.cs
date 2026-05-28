using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace online_shop_IT.Models;

public class Order
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    
}