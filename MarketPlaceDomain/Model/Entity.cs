namespace MarketPlaceDomain.Model;

public abstract class Entity
{
    public string Id { get; set; } = Guid.NewGuid().ToString(); 
}