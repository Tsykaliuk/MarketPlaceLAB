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

namespace MarketPlaceInfrastructure.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly OlxContext _context;
        private readonly IWebHostEnvironment _env;

        public CategoriesController(OlxContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            return View(await _context.Categories.ToListAsync());
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return RedirectToAction("Index", "Products", new { categoryId = category.Id });
        }


        // GET: Categories/Create   
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Id")] Category category, IFormFile CategoryImage)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            if (CategoryImage != null && CategoryImage.Length > 0)
            {
                var fileName = Path.GetFileName(CategoryImage.FileName);
                var uniqueFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}{Path.GetExtension(fileName)}";

                var imagesFolder = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                }

                var filePath = Path.Combine(imagesFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await CategoryImage.CopyToAsync(stream);
                }

                category.ImageUrl = $"/images/{uniqueFileName}";
            }

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Categories/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, Category category, IFormFile CategoryImage)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            var existing = await _context.Categories.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(existing);
            }

            existing.Name = category.Name;

            if (CategoryImage != null && CategoryImage.Length > 0)
            {
                if (!string.IsNullOrEmpty(existing.ImageUrl))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, existing.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                var fileName = Path.GetFileName(CategoryImage.FileName);
                var uniqueName = $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}{Path.GetExtension(fileName)}";

                var imagesFolder = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                }

                var newPath = Path.Combine(imagesFolder, uniqueName);
                using (var stream = new FileStream(newPath, FileMode.Create))
                {
                    await CategoryImage.CopyToAsync(stream);
                }

                existing.ImageUrl = $"/images/{uniqueName}";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }



        // GET: Categories/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            if (!string.IsNullOrEmpty(category.ImageUrl))
            {
                var imagePath = Path.Combine(_env.WebRootPath, category.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(string id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}
