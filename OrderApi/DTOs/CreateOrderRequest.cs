
using System.ComponentModel.DataAnnotations;

namespace OrderApi.DTOs;

public class CreateOrderRequest
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateOrderItemRequest> Items { get; set; }
        = new();
}

public class CreateOrderItemRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 10000)]
    public int Quantity { get; set; }
}
