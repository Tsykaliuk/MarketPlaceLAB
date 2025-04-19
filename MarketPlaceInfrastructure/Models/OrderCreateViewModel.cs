using System.ComponentModel.DataAnnotations;

public class OrderCreateViewModel
{
    [Required]
    public string ProductId { get; set; }

    public string ProductName { get; set; } 

    [Display(Name = "Ціна за одиницю")]
    public decimal ProductPrice { get; set; }

    public string ProductImageUrl { get; set; }

    [Display(Name = "Доступно")]
    public int MaxQuantity { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Кількість повинна бути щонайменше 1.")]
    [Display(Name = "Кількість")]
    public int Quantity { get; set; } = 1;
}