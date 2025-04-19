using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MarketPlaceDomain.Model;
using MarketPlaceInfrastructure;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MarketPlaceInfrastructure.Services;

namespace MarketPlaceInfrastructure.Controllers
{
    public class ProductsController : Controller
    {
        private readonly OlxContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IDataPortServiceFactory _dataPortServiceFactory; // Додайте фабрику
        private readonly ILogger<ProductsController> _logger; // Додайте логер

        public ProductsController(OlxContext context, IWebHostEnvironment env, IDataPortServiceFactory dataPortServiceFactory, ILogger<ProductsController> logger)
        {   
            _context = context;
            _env = env;
            _dataPortServiceFactory = dataPortServiceFactory; // Ініціалізуйте фабрику
            _logger = logger;
        }

        // GET: Products    
        public async Task<IActionResult> Index(string searchString, string categoryId, string view = "active") 
        {
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.User) 
                .AsQueryable();

            if (view == "archive")
            {
                productsQuery = productsQuery.Where(p => p.Status != ProductStatus.InStock);
                ViewBag.CurrentView = "archive"; 
            }
            else
            {
                
                productsQuery = productsQuery.Where(p => p.Status == ProductStatus.InStock);
                ViewBag.CurrentView = "active"; 
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p => p.Title.Contains(searchString) || p.Description.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(categoryId))
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId);
            }

            var products = await productsQuery
                                 .OrderByDescending(p => p.CreatedAt) 
                                 .ToListAsync();

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

            return View(products);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }
            ViewBag.CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return View(product);
        }

        // GET: Products/Create
        [Authorize]
        public IActionResult Create(string categoryId)
        {
            if (string.IsNullOrEmpty(categoryId))
            {
                return BadRequest("CategoryId is required.");
            }

            var category = _context.Categories.FirstOrDefault(c => c.Id == categoryId);
            if (category == null)
            {
                return NotFound($"Category with ID '{categoryId}' not found.");
            }

            var categories = _context.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.Id,
                    Text = c.Name   
                }).ToList();

            ViewData["CategoryId"] = new SelectList(categories, "Value", "Text", categoryId);

            ViewBag.CategoryName = category.Name;

            var product = new Product
            {
                CategoryId = categoryId
            };

            return View(product);
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string categoryId,
            [Bind("Title,Description,Price,CategoryId,Stock")] Product product,
            List<IFormFile> ProductImages
        )
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            product.UserId = userId;
            product.CategoryId = categoryId;
            product.CreatedAt = DateTime.UtcNow;
            product.Status = ProductStatus.InStock;


            if (!ModelState.IsValid)
            {
                return View(product);
            }

            _context.Add(product);
            await _context.SaveChangesAsync();

            if (ProductImages != null && ProductImages.Count > 0)
            {
                var imagesFolder = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                }

                foreach (var formFile in ProductImages)
                {
                    if (formFile.Length > 0)
                    {
                        var originalName = Path.GetFileName(formFile.FileName);
                        var uniqueName = $"{Path.GetFileNameWithoutExtension(originalName)}_{Guid.NewGuid()}{Path.GetExtension(originalName)}";
                        var filePath = Path.Combine(imagesFolder, uniqueName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await formFile.CopyToAsync(stream);
                        }

                        var image = new Image
                        {
                            Id = Guid.NewGuid().ToString(),
                            ImageUrl = $"/images/{uniqueName}",
                            ProductId = product.Id
                        };

                        _context.Images.Add(image);
                    }
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Products", new { id = categoryId });
        }


        // GET: Products/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //var product = await _context.Products.FindAsync(id);
            var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (product.UserId != userId)
            {
                return Forbid();
            }

            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(
            string id,
            [Bind("Id,Title,Description,Price,CategoryId,Status,Stock")] Product product,
            List<string>? RemoveImages,
            List<IFormFile>? NewImages
        )
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            var existingProduct = await _context.Products
                .Include(p => p.Images) 
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingProduct == null)
            {
                return NotFound();
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (existingProduct.UserId != userId)
            {
                return Forbid();
            }

            existingProduct.Title = product.Title;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.Status = product.Status;

            if (RemoveImages != null && RemoveImages.Any())
            {
                foreach (var imageId in RemoveImages)
                {
                    var img = existingProduct.Images.FirstOrDefault(x => x.Id == imageId);
                    if (img != null)
                    {
                        var filePath = Path.Combine(_env.WebRootPath, img.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                        _context.Images.Remove(img);
                    }
                }
            }

            if (NewImages != null && NewImages.Count > 0)
            {
                var imagesFolder = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                }

                foreach (var formFile in NewImages)
                {
                    if (formFile.Length > 0)
                    {
                        var originalName = Path.GetFileName(formFile.FileName);
                        var uniqueName = $"{Path.GetFileNameWithoutExtension(originalName)}_{Guid.NewGuid()}{Path.GetExtension(originalName)}";
                        var filePath = Path.Combine(imagesFolder, uniqueName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await formFile.CopyToAsync(stream);
                        }

                        var newImg = new Image
                        {
                            Id = Guid.NewGuid().ToString(),
                            ImageUrl = $"/images/{uniqueName}",
                            ProductId = existingProduct.Id
                        };
                        _context.Images.Add(newImg);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // GET: Products/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && product.UserId != userId)
            {
                return Forbid();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(string id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        [Authorize]
        public IActionResult SelectCategory()
        {
            // Отримуємо список всіх категорій
            var categories = _context.Categories.ToList();
            return View(categories);
        }

        [HttpGet]
        public async Task<IActionResult> Export(CancellationToken cancellationToken)
        {
            try
            {
                var exportService = _dataPortServiceFactory.GetExportService<Product>();
                byte[] fileContents = await exportService.ExportToByteArrayAsync(cancellationToken);

                string fileName = $"products_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                _logger.LogInformation("Exporting products to Excel file: {FileName}", fileName);
                return File(fileContents, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during product export.");
                TempData["ExportError"] = "An error occurred while exporting data. Please try again later.";
                return RedirectToAction(nameof(Index)); // Перенаправляємо назад на Index
            }
        }

        /// <summary>
        /// Imports products from an uploaded Excel file.
        /// </summary>
        /// <param name="file">The uploaded Excel file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPost]
        [ValidateAntiForgeryToken] // Важливо для безпеки POST запитів з формами
        public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ImportError"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Index));
            }

            string fileExtension = Path.GetExtension(file.FileName);
            if (fileExtension?.ToLowerInvariant() != ".xlsx")
            {
                TempData["ImportError"] = "Invalid file format. Please upload an .xlsx file.";
                return RedirectToAction(nameof(Index));
            }

            // --- Отримання ID поточного користувача ---
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // Якщо користувач не автентифікований, UserId буде null або порожнім
                _logger.LogWarning("Import attempt failed: User is not authenticated or UserId claim is missing.");
                TempData["ImportError"] = "Authentication error. Please log in again to import products.";
                // Поверніть на сторінку входу або Index
                return RedirectToAction("Login", "Account"); // Приклад перенаправлення на логін
            }
            // --------------------------------------

            try
            {
                _logger.LogInformation("Starting import from file: {FileName} for User ID: {UserId}", file.FileName, userId);
                var importService = _dataPortServiceFactory.GetImportService<Product>();

                using (var stream = file.OpenReadStream())
                {
                    // --- Створення словника параметрів та передача UserId ---
                    var importParams = new Dictionary<string, object>
                    {
                        { "UserId", userId } // Ключ має співпадати з тим, що очікує сервіс
                    };
                    await importService.ImportFromStreamAsync(stream, importParams, cancellationToken);
                    // ------------------------------------------------------
                }

                // Це повідомлення тепер більш оптимістичне. Успіх означає, що процес пройшов без ВИнятків.
                TempData["ImportMessage"] = "Product import process completed. Check logs for details.";
                _logger.LogInformation("Finished import attempt from file: {FileName} for User ID: {UserId}", file.FileName, userId);
            }
            // --- Обробка конкретних помилок від сервісу ---
            catch (InvalidOperationException ex) // Ловить помилки збереження БД або інші операційні помилки
            {
                _logger.LogError(ex, "Import failed for file {FileName} due to operation error.", file.FileName);
                // Показуємо користувачу деталі помилки з InvalidOperationException
                TempData["ImportError"] = $"Import failed: {ex.Message}";
            }
            catch (ArgumentException ex) // Ловить помилку відсутності UserId
            {
                _logger.LogError(ex, "Import failed for file {FileName} due to argument error (e.g., missing UserId).", file.FileName);
                TempData["ImportError"] = $"Import setup failed: {ex.Message}";
            }
            catch (InvalidDataException ex) // Ловить помилку відсутності стовпців
            {
                _logger.LogWarning(ex, "Import failed for file {FileName} due to missing columns.", file.FileName);
                TempData["ImportError"] = $"Import failed: {ex.Message}";
            }
            // --------------------------------------------
            catch (Exception ex) // Загальна помилка
            {
                _logger.LogError(ex, "An unexpected error occurred during product import from file {FileName}.", file.FileName);
                TempData["ImportError"] = "An unexpected error occurred during import. Please check the file or contact support.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
