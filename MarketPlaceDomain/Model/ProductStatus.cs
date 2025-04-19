using System.ComponentModel.DataAnnotations;

namespace MarketPlaceDomain.Model
{
    public enum ProductStatus
    {
        [Display(Name = "Є в наявності")]
        InStock,

        [Display(Name = "Немає в наявності")]
        OutOfStock,

        [Display(Name = "Архівовано")]
        Archived
    }
}
