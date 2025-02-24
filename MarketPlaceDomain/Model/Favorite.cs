﻿using System;
using System.Collections.Generic;

namespace MarketPlaceDomain.Model;

public partial class Favorite : Entity
{
    public int UserId { get; set; }

    public int ProductId { get; set; }

    public DateTime? AddedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
