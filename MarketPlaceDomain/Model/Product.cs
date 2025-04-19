using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MarketPlaceDomain.Model;

public partial class Product : Entity
{
    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    [Display(Name = "Назва")]
    public string Title { get; set; } = null!;
    [Display(Name = "Опис")]
    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    public string Description { get; set; } = null!;
    [Display(Name = "Ціна ₴")]
    public decimal Price { get; set; }
    [ValidateNever]
    public string? UserId { get; set; } = null!;
    [ValidateNever]
    public string? CategoryId { get; set; } = null!;
    [Display(Name = "Статус")]
    public ProductStatus Status { get; set; } = ProductStatus.InStock;
    [Display(Name = "Додано")]
    public int Stock { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [ValidateNever]
    [Display(Name = "Категорія")]
    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public virtual ICollection<Image> Images { get; set; } = new List<Image>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    [ValidateNever]
    [Display(Name = "Власник")]
    public ApplicationUser User { get; set; } = null!;
}
