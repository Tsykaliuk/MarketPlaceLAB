    namespace MarketPlaceDomain.Model;

public partial class Favorite : Entity
{
    public string UserId { get; set; } = null!;
    public string ProductId { get; set; } = null!;
    public DateTime? AddedAt { get; set; } = DateTime.UtcNow;
    public virtual Product Product { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
