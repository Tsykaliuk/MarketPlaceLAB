using Microsoft.AspNetCore.Identity;

namespace MarketPlaceDomain.Model;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; }
    public string Surname { get; set; }
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}   