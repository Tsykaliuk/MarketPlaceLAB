using System;
using System.Collections.Generic;

namespace MarketPlaceDomain.Model;

public partial class Order : Entity
{
    public int? UserId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal TotalPrice { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User? User { get; set; }
}
