using MarketPlaceDomain.Model;

public class ProfileViewModel
{
    public List<Product> MyProducts { get; set; } = new();
    public List<Order> MyOrders { get; set; } = new();
    public ApplicationUser User { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<Order> IncomingOrders { get; set; } = new List<Order>();
}