using System;
using System.Collections.Generic;

namespace MarketPlaceDomain.Model;

public partial class Role : Entity
{
    public string RoleName { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
