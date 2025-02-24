using System;
using System.Collections.Generic;

namespace MarketPlaceDomain.Model;

public partial class Image : Entity
{
    public string ImageUrl { get; set; } = null!;

    public int ProductId { get; set; }

    public virtual Product Product { get; set; } = null!;
}
