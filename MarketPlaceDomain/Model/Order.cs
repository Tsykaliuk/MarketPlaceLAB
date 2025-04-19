using System;
using System.Collections.Generic;

namespace MarketPlaceDomain.Model;

public partial class Order : Entity
{
    public string UserId { get; set; } = null!;
    public string ProductId { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public virtual Product Product { get; set; } = null!;
    public virtual ApplicationUser User { get; set; } = null!;
}
