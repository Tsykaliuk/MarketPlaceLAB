using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MarketPlaceInfrastructure.Controllers;
public class ProductsInCategoryPriceAboveViewModel
{
    [Display(Name = "Категорія")]
    public string? SelectedCategoryId { get; set; }

    [Display(Name = "Мінімальна ціна")]
    [DataType(DataType.Currency)]
    [Range(0, double.MaxValue, ErrorMessage = "Ціна не може бути від'ємною")]
    public decimal MinPrice { get; set; }

    public SelectList? Categories { get; set; }
    public List<ProductInfo>? Results { get; set; }
    public class ProductInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}

public class ProductsInCategoryOrderedMinTimesViewModel
{
    [Display(Name = "Категорія")]
    public string? SelectedCategoryId { get; set; }

    [Display(Name = "Мінімальна кількість замовлень")]
    [Range(1, int.MaxValue, ErrorMessage = "Кількість повинна бути більше 0")]
    public int MinOrderCount { get; set; } = 1;

    public SelectList? Categories { get; set; }
    public List<ProductOrderCountResult>? Results { get; set; }

    public class ProductOrderCountResult
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductTitle { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int ActualOrderCount { get; set; }
    }
}

public class UserOrdersAfterDateViewModel
{
    [Display(Name = "Користувач (покупець)")]
    public string? SelectedUserId { get; set; }

    [Display(Name = "Початкова дата")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    public SelectList? Users { get; set; }
    public List<OrderInfo>? Results { get; set; }
    public class OrderInfo
    {
        public string OrderId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
public class ProductsOrderedByUserMoreThanNTimesViewModel
{
    [Display(Name = "Користувач (покупець)")]
    public string? SelectedUserId { get; set; }

    [Display(Name = "Мінімальна кількість замовлень одного товару")]
    [Range(1, int.MaxValue, ErrorMessage = "Кількість повинна бути більше 0")]
    public int MinOrderCount { get; set; } = 1;

    public SelectList? Users { get; set; }
    public List<ProductOrderCountInfo>? Results { get; set; }
    public class ProductOrderCountInfo
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductTitle { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}


public class TotalOrderValueByProductInDateRangeViewModel
{
    [Display(Name = "Початкова дата")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);

    [Display(Name = "Кінцева дата")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today;

    public List<ProductOrderValueInfo>? Results { get; set; }
    public class ProductOrderValueInfo
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
    }
}

public class SellersWithSameCategorySetViewModel
{
    [Display(Name = "Еталонний продавець")]
    public string? ReferenceSellerId { get; set; }
    public SelectList? Sellers { get; set; }
    public List<SellerInfo>? Results { get; set; }
    public class SellerInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}

public class UsersOrderedFromAllCategoriesViewModel
{
    public List<SellersWithSameCategorySetViewModel.SellerInfo>? Results { get; set; }
}

public class SellerPairsSellingInSameCategoriesViewModel
{
    public List<SellerPairCategoryInfo>? Results { get; set; }
    public class SellerPairCategoryInfo
    {
        public string Seller1Name { get; set; } = string.Empty;
        public string Seller2Name { get; set; } = string.Empty;
        public List<string> SharedCategoryNames { get; set; } = new List<string>();
    }
}


public class QueriesController : Controller
{
    private readonly OlxContext _context;

    public QueriesController(OlxContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }


    // 1: Products in a specific category with a price greater than X.
    [HttpGet]
    public async Task<IActionResult> ProductsInCategoryPriceAbove()
    {
        var viewModel = new ProductsInCategoryPriceAboveViewModel
        {
            Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name")
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductsInCategoryPriceAbove(ProductsInCategoryPriceAboveViewModel model)
    {
        model.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", model.SelectedCategoryId);
        if (ModelState.IsValid && !string.IsNullOrEmpty(model.SelectedCategoryId))
        {
            model.Results = await _context.Products
                .Where(p => p.CategoryId == model.SelectedCategoryId && p.Price > model.MinPrice)
                .Include(p => p.Category)
                .Select(p => new ProductsInCategoryPriceAboveViewModel.ProductInfo
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    Price = p.Price,
                    CategoryName = p.Category.Name
                }).ToListAsync();
        }
        return View(model);
    }

    // 2: Orders placed by a specific user after a certain date
    [HttpGet]
    public async Task<IActionResult> UserOrdersAfterDate()
    {
        var viewModel = new UserOrdersAfterDateViewModel
        {
            Users = new SelectList(await _context.Users.OrderBy(u => u.UserName).ToListAsync(), "Id", "UserName")
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserOrdersAfterDate(UserOrdersAfterDateViewModel model)
    {
        model.Users = new SelectList(await _context.Users.OrderBy(u => u.UserName).ToListAsync(), "Id", "UserName", model.SelectedUserId);
        if (ModelState.IsValid && !string.IsNullOrEmpty(model.SelectedUserId))
        {
            model.Results = await _context.Orders
                .Where(o => o.UserId == model.SelectedUserId && o.CreatedAt >= model.StartDate)
                .Include(o => o.Product)
                .Select(o => new UserOrdersAfterDateViewModel.OrderInfo
                {
                    OrderId = o.Id,
                    ProductName = o.Product.Title,
                    Quantity = o.Quantity,
                    TotalPrice = o.TotalPrice,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status.ToString()
                }).ToListAsync();
        }
        return View(model);
    }

    // 3: Products ordered by a user more than N times.
    [HttpGet]
    public async Task<IActionResult> ProductsOrderedByUserMoreThanNTimes()
    {
        var viewModel = new ProductsOrderedByUserMoreThanNTimesViewModel
        {
            Users = new SelectList(await _context.Users.OrderBy(u => u.UserName).ToListAsync(), "Id", "UserName")
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductsOrderedByUserMoreThanNTimes(ProductsOrderedByUserMoreThanNTimesViewModel model)
    {
        model.Users = new SelectList(await _context.Users.OrderBy(u => u.UserName).ToListAsync(), "Id", "UserName", model.SelectedUserId);
        if (ModelState.IsValid && !string.IsNullOrEmpty(model.SelectedUserId))
        {
            model.Results = await _context.Orders
                .Where(o => o.UserId == model.SelectedUserId)
                .Include(o => o.Product)
                .ThenInclude(p => p.Category)
                .GroupBy(o => new { o.ProductId, o.Product.Title, CategoryName = o.Product.Category.Name })
                .Select(g => new ProductsOrderedByUserMoreThanNTimesViewModel.ProductOrderCountInfo
                {
                    ProductId = g.Key.ProductId,
                    ProductTitle = g.Key.Title,
                    OrderCount = g.Count(),
                    CategoryName = g.Key.CategoryName
                })
                .Where(r => r.OrderCount >= model.MinOrderCount)
                .OrderByDescending(r => r.OrderCount)
                .ToListAsync();
        }
        return View(model);
    }

    // 4: Products from a selected category, ordered at least N times
    [HttpGet]
    public async Task<IActionResult> ProductsInCategoryOrderedMinTimes()
    {
        var viewModel = new ProductsInCategoryOrderedMinTimesViewModel
        {
            Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name")
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductsInCategoryOrderedMinTimes(ProductsInCategoryOrderedMinTimesViewModel model)
    {
        model.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", model.SelectedCategoryId);
        if (ModelState.IsValid && !string.IsNullOrEmpty(model.SelectedCategoryId))
        {
            model.Results = await _context.Products
                .Where(p => p.CategoryId == model.SelectedCategoryId)
                .Include(p => p.Category)
                .Select(p => new {
                    Product = p,
                    OrderCount = p.Orders.Count()
                })
                .Where(x => x.OrderCount >= model.MinOrderCount)
                .OrderByDescending(x => x.OrderCount)
                .Select(x => new ProductsInCategoryOrderedMinTimesViewModel.ProductOrderCountResult
                {
                    ProductId = x.Product.Id,
                    ProductTitle = x.Product.Title,
                    CategoryName = x.Product.Category.Name,
                    ActualOrderCount = x.OrderCount
                })
                .ToListAsync();
        }
        return View(model);
    }

    // 5: Total order value for each product within a specific date range.
    [HttpGet]
    public IActionResult TotalOrderValueByProductInDateRange()
    {
        var viewModel = new TotalOrderValueByProductInDateRangeViewModel();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TotalOrderValueByProductInDateRange(TotalOrderValueByProductInDateRangeViewModel model)
    {
        if (ModelState.IsValid)
        {
            if (model.StartDate > model.EndDate)
            {
                ModelState.AddModelError("EndDate", "Кінцева дата не може бути раніше початкової.");
            }
            else
            {
                model.Results = await _context.Orders
                    .Where(o => o.CreatedAt >= model.StartDate && o.CreatedAt <= model.EndDate)
                    .Include(o => o.Product)
                    .GroupBy(o => o.Product.Title)
                    .Select(g => new TotalOrderValueByProductInDateRangeViewModel.ProductOrderValueInfo
                    {
                        ProductName = g.Key,
                        TotalValue = g.Sum(o => o.TotalPrice)
                    })
                    .OrderByDescending(r => r.TotalValue)
                    .ToListAsync();
            }
        }
        return View(model);
    }



    // 6: Sellers who sell products in the exact same set of categories as a reference seller.
    [HttpGet]
    public async Task<IActionResult> SellersWithSameCategorySet()
    {
        var viewModel = new SellersWithSameCategorySetViewModel
        {
            Sellers = new SelectList(await _context.Users.OrderBy(u => u.UserName).ToListAsync(), "Id", "UserName")
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SellersWithSameCategorySet(SellersWithSameCategorySetViewModel model)
    {
        model.Sellers = new SelectList(await _context.Users.OrderBy(u => u.UserName).ToListAsync(), "Id", "UserName", model.ReferenceSellerId);
        if (ModelState.IsValid && !string.IsNullOrEmpty(model.ReferenceSellerId))
        {
            var referenceSellerCategoryIds = await _context.Products
                .Where(p => p.UserId == model.ReferenceSellerId && p.CategoryId != null)
                .Select(p => p.CategoryId)
                .Distinct()
                .OrderBy(id => id)
                .ToListAsync();

            if (!referenceSellerCategoryIds.Any())
            {
                ModelState.AddModelError("", "Обраний еталонний продавець не має товарів у категоріях або не продає товари.");
                return View(model);
            }

            var allOtherSellers = await _context.Users
                                 .Where(u => u.Id != model.ReferenceSellerId)
                                 .ToListAsync();

            var resultSellers = new List<SellersWithSameCategorySetViewModel.SellerInfo>();

            foreach (var seller in allOtherSellers)
            {
                var currentSellerCategoryIds = await _context.Products
                    .Where(p => p.UserId == seller.Id && p.CategoryId != null)
                    .Select(p => p.CategoryId)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToListAsync();

                if (referenceSellerCategoryIds.SequenceEqual(currentSellerCategoryIds))
                {
                    resultSellers.Add(new SellersWithSameCategorySetViewModel.SellerInfo
                    {
                        UserId = seller.Id,
                        UserName = seller.UserName ?? "N/A",
                        Email = seller.Email ?? "N/A"
                    });
                }
            }
            model.Results = resultSellers;
        }
        return View(model);
    }

    // 7: Users who have ordered products from ALL available categories.
    [HttpGet]
    public async Task<IActionResult> UsersOrderedFromAllCategories()
    {
        var model = new UsersOrderedFromAllCategoriesViewModel();
        var allCategoryIds = await _context.Categories.Select(c => c.Id).Distinct().ToListAsync();

        if (!allCategoryIds.Any())
        {
            ModelState.AddModelError("", "В системі немає категорій для порівняння.");
            return View(model);
        }

        var users = await _context.Users.ToListAsync();
        var resultUsers = new List<SellersWithSameCategorySetViewModel.SellerInfo>();

        foreach (var user in users)
        {
            var userOrderedCategoryIds = await _context.Orders
                .Where(o => o.UserId == user.Id && o.Product.CategoryId != null)
                .Select(o => o.Product.CategoryId)
                .Distinct()
                .ToListAsync();

            bool orderedFromAll = !allCategoryIds.Except(userOrderedCategoryIds!).Any();


            if (orderedFromAll)
            {
                resultUsers.Add(new SellersWithSameCategorySetViewModel.SellerInfo
                {
                    UserId = user.Id,
                    UserName = user.UserName ?? "N/A",
                    Email = user.Email ?? "N/A"
                });
            }
        }
        model.Results = resultUsers;
        return View(model);
    }


    // 8: Pairs of sellers who sell products in at least one common category.
    [HttpGet]
    public async Task<IActionResult> SellerPairsSellingInSameCategories()
    {
        var model = new SellerPairsSellingInSameCategoriesViewModel();

        var productSellerCategoryEntries = await _context.Products
            .Where(p => p.UserId != null && p.User != null &&
                        p.CategoryId != null && p.Category != null)
            .Select(p => new
            {
                SellerId = p.UserId,
                SellerName = p.User.UserName,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name
            })
            .ToListAsync();

        var sellersWithTheirCategories = productSellerCategoryEntries
            .GroupBy(entry => new { entry.SellerId, entry.SellerName })
            .Select(group => new
            {
                SellerId = group.Key.SellerId,
                SellerName = group.Key.SellerName,
                CategoryDetails = group
                                    .Select(item => new { Id = item.CategoryId, Name = item.CategoryName })
                                    .DistinctBy(cd => cd.Id)
                                    .ToList()
            })
            .Where(s => s.CategoryDetails.Any())
            .ToList();

        var resultPairs = new List<SellerPairsSellingInSameCategoriesViewModel.SellerPairCategoryInfo>();

        for (int i = 0; i < sellersWithTheirCategories.Count; i++)
        {
            for (int j = i + 1; j < sellersWithTheirCategories.Count; j++)
            {
                var seller1Data = sellersWithTheirCategories[i];
                var seller2Data = sellersWithTheirCategories[j];

                var seller1CategoryIds = seller1Data.CategoryDetails.Select(cd => cd.Id).ToList();
                var seller2CategoryIds = seller2Data.CategoryDetails.Select(cd => cd.Id).ToList();

                var commonCategoryIds = seller1CategoryIds.Intersect(seller2CategoryIds).ToList();

                if (commonCategoryIds.Any())
                {
                    var commonCategoryNames = seller1Data.CategoryDetails
                        .Where(cd => commonCategoryIds.Contains(cd.Id!))
                        .Select(cd => cd.Name!)
                        .Distinct()
                        .ToList();

                    resultPairs.Add(new SellerPairsSellingInSameCategoriesViewModel.SellerPairCategoryInfo
                    {
                        Seller1Name = seller1Data.SellerName ?? "N/A",
                        Seller2Name = seller2Data.SellerName ?? "N/A",
                        SharedCategoryNames = commonCategoryNames
                    });
                }
            }
        }
        model.Results = resultPairs;
        return View(model);
    }
}