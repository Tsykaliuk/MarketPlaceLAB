using System.ComponentModel.DataAnnotations;
namespace MarketPlaceDomain.Model;
public partial class Category : Entity
{
    [Required(ErrorMessage="Поле не повинно бути порожнім")]
    [Display(Name = "Категорія")]
    public string Name { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
