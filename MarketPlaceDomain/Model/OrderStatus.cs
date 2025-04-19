using System.ComponentModel.DataAnnotations;

namespace MarketPlaceDomain.Model
{
    public enum OrderStatus
    {
        [Display(Name = "Очікує")]
        Pending,

        [Display(Name = "Підтверджено")]
        Confirmed,

        [Display(Name = "Відправлено")]
        Shipped,

        [Display(Name = "Доставлено")]
        Delivered,

        [Display(Name = "Скасовано")]
        Canceled
    }
}