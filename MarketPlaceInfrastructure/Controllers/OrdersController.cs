using System.Security.Claims;
using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MarketPlaceDomain.Model;
using MarketPlaceInfrastructure.Services; 
using MarketPlaceInfrastructure;

public class OrdersController : Controller
{
    private readonly OlxContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersController(OlxContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        IQueryable<Order> ordersQuery;

        if (User.IsInRole("Admin")) 
        {
            ordersQuery = _context.Orders
                            .Include(o => o.Product)
                            .Include(o => o.User)
                            .OrderByDescending(o => o.CreatedAt);
        }
        else // Користувач бачить тільки свої
        {
            ordersQuery = _context.Orders
                            .Where(o => o.UserId == userId)
                            .Include(o => o.Product) 
                            .OrderByDescending(o => o.CreatedAt);
        }

        return View(await ordersQuery.ToListAsync());
    }

    // GET: Orders/Details/5
    public async Task<IActionResult> Details(string id)
    {
        if (id == null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var order = await _context.Orders
            .Include(o => o.Product)
                .ThenInclude(p => p.Images)
            .Include(o => o.Product)
                .ThenInclude(p => p.User) 
            .Include(o => o.User) 
            .FirstOrDefaultAsync(m => m.Id == id);


        if (order == null) return NotFound();

        //if (order.UserId != userId && !User.IsInRole("Admin"))
        //{
        //    return Forbid(); 
        //}

        return View(order);
    }

    // GET: Orders/Create
    public async Task<IActionResult> Create(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return BadRequest("ProductId is required");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var product = await _context.Products
            .Include(p => p.Images) 
            .FirstOrDefaultAsync(p => p.Id == productId);

        
        if (product == null) return NotFound("Товар не знайдено.");
        if (product.UserId == userId) return BadRequest("Ви не можете замовити власний товар.");
        if (product.Status != ProductStatus.InStock || product.Stock <= 0) return BadRequest("Товару немає в наявності.");

        var vm = new OrderCreateViewModel
        {
            ProductId = product.Id,
            ProductName = product.Title,
            ProductPrice = product.Price,
            ProductImageUrl = product.Images?.FirstOrDefault()?.ImageUrl ?? "/images/noimage.jpeg", 
            MaxQuantity = product.Stock, 
            Quantity = 1 
        };

        return View(vm);
    }


    // POST: Orders/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var productForView = await _context.Products.AsNoTracking().Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == vm.ProductId);
            if (productForView != null)
            {
                vm.ProductName = productForView.Title;
                vm.ProductPrice = productForView.Price;
                vm.ProductImageUrl = productForView.Images?.FirstOrDefault()?.ImageUrl ?? "/images/noimage.jpeg";
                vm.MaxQuantity = productForView.Stock;
            }
            else
            {
                ModelState.AddModelError("", "Товар більше не доступний.");
                return View(vm); 
            }
            return View(vm);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == vm.ProductId);

            if (product == null)
            {
                ModelState.AddModelError("", "Товар не знайдено.");
                await transaction.RollbackAsync();
                return View(vm); 
            }
            if (product.UserId == userId)
            {
                ModelState.AddModelError("", "Ви не можете замовити власний товар.");
                await transaction.RollbackAsync();
                return View(vm); 
            }
            if (product.Status != ProductStatus.InStock || product.Stock < vm.Quantity)
            {
                ModelState.AddModelError("Quantity", $"Доступно лише {product.Stock} од.");
                await transaction.RollbackAsync();
                vm.ProductName = product.Title;
                vm.ProductPrice = product.Price;
                vm.ProductImageUrl = (await _context.Images.FirstOrDefaultAsync(i => i.ProductId == product.Id))?.ImageUrl ?? "/images/noimage.jpeg";
                vm.MaxQuantity = product.Stock;
                return View(vm);
            }

            product.Stock -= vm.Quantity;
            if (product.Stock == 0)
            {
                product.Status = ProductStatus.OutOfStock;
            }
            _context.Update(product); 

            var order = new Order
            {
                Id = Guid.NewGuid().ToString(), 
                UserId = userId,
                ProductId = product.Id,
                Quantity = vm.Quantity,
                TotalPrice = product.Price * vm.Quantity, 
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            _context.Orders.Add(order);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction("Details", new { id = order.Id });
        }
        catch (Exception ex) 
        {
            await transaction.RollbackAsync(); 
            ModelState.AddModelError("", "Не вдалося створити замовлення. Спробуйте пізніше.");
            var productForView = await _context.Products.AsNoTracking().Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == vm.ProductId);
            if (productForView != null)
            {
                vm.ProductName = productForView.Title;
                vm.ProductPrice = productForView.Price;
                vm.ProductImageUrl = productForView.Images?.FirstOrDefault()?.ImageUrl ?? "/images/noimage.jpeg";
                vm.MaxQuantity = productForView.Stock;
            }
            return View(vm);
        }
    }

    // GET: Orders/Edit/5 (Зміна статусу адміном)
    [Authorize(Roles = "Admin")] 
    public async Task<IActionResult> Edit(string id)
    {
        if (id == null) return NotFound();

        var order = await _context.Orders
                            .Include(o => o.Product) // Включити продукт корисно
                            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(OrderStatus))
                                             .Cast<OrderStatus>()
                                             .Select(v => new SelectListItem
                                             {
                                                 Text = EnumExtensions.GetDisplayName(v), // Використовуємо хелпер для назв
                                                 Value = v.ToString()
                                             }), "Value", "Text", order.Status); // Передаємо поточний статус

        return View(order);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, OrderStatus status) // Приймаємо тільки ID і новий статус
    {
        if (id == null) return NotFound();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders.Include(o => o.Product).FirstOrDefaultAsync(o => o.Id == id); // Завантажуємо замовлення та товар
            if (order == null)
            {
                await transaction.RollbackAsync();
                return NotFound();
            }

            var previousStatus = order.Status; // Зберігаємо попередній статус

            order.Status = status;
            _context.Update(order);

            if (previousStatus != OrderStatus.Canceled && status == OrderStatus.Canceled)
            {
                if (order.Product != null) // Перевірка, чи товар не видалено
                {
                    order.Product.Stock += order.Quantity;
                    if (order.Product.Status == ProductStatus.OutOfStock && order.Product.Stock > 0)
                    {
                        order.Product.Status = ProductStatus.InStock;
                    }
                    _context.Update(order.Product);
                }
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction("Details", new { id = order.Id }); // Повертаємось на деталі
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Помилка при оновленні статусу."; // Повідомлення про помилку
            return RedirectToAction("Edit", new { id = id }); // Повертаємось на сторінку редагування
        }
    }


    [Authorize(Roles = "Admin")] // Додати авторизацію
    public async Task<IActionResult> Delete(string id)
    {
        if (id == null) return NotFound();
        var order = await _context.Orders
            .Include(o => o.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (order == null) return NotFound();
        return View(order); // Повертаємо View
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")] // Додати авторизацію
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order != null)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index)); // Редірект на список всіх замовлень (для адміна)
    }

    // GET: Orders/EditSellerStatus/5
    [Authorize] // Продавець має бути залогінений
    public async Task<IActionResult> EditSellerStatus(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var order = await _context.Orders
                            .Include(o => o.Product) // Потрібен продукт для перевірки власника
                            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        if (order.Product == null || order.Product.UserId != userId)
        {
            return Forbid(); // Або return RedirectToAction("AccessDenied", "Account");
        }

        var allowedStatuses = new List<OrderStatus>();
        switch (order.Status)
        {
            case OrderStatus.Pending:
                allowedStatuses.Add(OrderStatus.Confirmed);
                allowedStatuses.Add(OrderStatus.Canceled);
                break;
            case OrderStatus.Confirmed:
                allowedStatuses.Add(OrderStatus.Shipped);
                allowedStatuses.Add(OrderStatus.Canceled); // Можливо, дозволити скасування і після підтвердження?
                break;
            case OrderStatus.Shipped:
                break;
        }

        ViewBag.Statuses = new SelectList(allowedStatuses.Select(v => new SelectListItem
        {
            Text = EnumExtensions.GetDisplayName(v),
            Value = v.ToString()
        }), "Value", "Text"); // Не передаємо selected value, щоб нічого не було вибрано за замовчуванням

        ViewBag.CurrentStatus = order.Status;
        ViewBag.CurrentStatusName = EnumExtensions.GetDisplayName(order.Status);

        return View("Edit", order);
    }

    // POST: Orders/EditSellerStatus/5
    [HttpPost]
    [Authorize] // Потрібен логін
    [ValidateAntiForgeryToken]
    [ActionName("EditSellerStatus")] // Вказуємо, що цей метод обробляє дію EditSellerStatus
    public async Task<IActionResult> EditSellerStatusPost(string id, OrderStatus status) // Приймаємо ID та новий статус
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                                .Include(o => o.Product) // Завантажуємо товар для перевірки прав та оновлення залишків
                                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                await transaction.RollbackAsync();
                return NotFound();
            }

            if (order.Product == null || order.Product.UserId != userId)
            {
                await transaction.RollbackAsync();
                return Forbid();
            }

            var previousStatus = order.Status;
            bool isValidTransition = false;
            switch (previousStatus)
            {
                case OrderStatus.Pending:
                    isValidTransition = (status == OrderStatus.Confirmed || status == OrderStatus.Canceled);
                    break;
                case OrderStatus.Confirmed:
                    isValidTransition = (status == OrderStatus.Shipped || status == OrderStatus.Canceled);
                    break;
            }

            if (!isValidTransition)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Неможливо змінити статус з '{EnumExtensions.GetDisplayName(previousStatus)}' на '{EnumExtensions.GetDisplayName(status)}'.";
                return RedirectToAction("EditSellerStatus", new { id = id }); // Повертаємо на GET сторінку з помилкою
            }


            order.Status = status;
            _context.Orders.Update(order); // Явно вказуємо, що замовлення змінилося

            if (previousStatus != OrderStatus.Canceled && status == OrderStatus.Canceled)
            {
                if (order.Product != null) // Додаткова перевірка, що товар існує
                {
                    order.Product.Stock += order.Quantity; // Повертаємо кількість
                                                           // Якщо товар був OutOfStock, але ми повертаємо одиниці, робимо InStock
                    if (order.Product.Status == ProductStatus.OutOfStock && order.Product.Stock > 0)
                    {
                        order.Product.Status = ProductStatus.InStock;
                    }
                    _context.Products.Update(order.Product); // Явно вказуємо, що товар змінився
                }
            }

            await _context.SaveChangesAsync(); // Зберігаємо зміни Order та Product
            await transaction.CommitAsync(); // Фіксуємо транзакцію

            TempData["SuccessMessage"] = "Статус замовлення успішно оновлено."; // Повідомлення про успіх
            return RedirectToAction("Profile", "Account"); // Повертаємось на сторінку профілю
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Сталася помилка при оновленні статусу.";
            return RedirectToAction("EditSellerStatus", new { id = id }); // Повертаємось на GET сторінку з помилкою
        }
    }
    private bool OrderExists(string id)
    {
        return _context.Orders.Any(e => e.Id == id);
    }
}