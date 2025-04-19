namespace MarketPlaceDomain.Model;

public partial class Image : Entity
{
    public string ImageUrl { get; set; } = null!;
    public string ProductId { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
