using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketPlaceDomain.Model;
using MarketPlaceInfrastructure.Services;

namespace MarketPlaceInfrastructure.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChartsController : ControllerBase
    {
        private readonly OlxContext _context;

        public ChartsController(OlxContext context)
        {
            _context = context;
        }

        [HttpGet("categoryProductCount")]
        public async Task<JsonResult> GetCategoryProductCountAsync()
        {
            var productCounts = await _context.Categories
                .Select(c => new
                {
                    CategoryName = c.Name,
                    ProductCount = c.Products.Count
                })
                .Where(d => d.ProductCount > 0)
                .OrderByDescending(d => d.ProductCount)
                .ToListAsync();

            return new JsonResult(productCounts);
        }

        [HttpGet("orderStatusDistribution")]
        public async Task<IActionResult> GetOrderStatusDistribution()
        {
            var data = await _context.Orders
               .GroupBy(o => o.Status)
               .Select(g => new
               {
                   Status = g.Key,
                   Count = g.Count()
               })
               .ToListAsync();

            var result = data.Select(d => new {
                StatusName = EnumExtensions.GetDisplayName(d.Status), 
                Count = d.Count
            }).ToList();


            return Ok(result);
        }

        [HttpGet("topProductsByQuantity")]
        public async Task<IActionResult> GetTopProductsByQuantity(int count = 5)
        {
            if (count <= 0) count = 5;

            var data = await _context.Orders
                .Where(o => o.Status != OrderStatus.Canceled)
                .GroupBy(o => o.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(o => o.Quantity) 
                })
                .OrderByDescending(x => x.TotalQuantity) 
                .Take(count) 
                .Join(_context.Products, 
                      orderGroup => orderGroup.ProductId,
                      product => product.Id,
                      (orderGroup, product) => new
                      {
                          ProductName = product.Title,
                          TotalQuantity = orderGroup.TotalQuantity
                      })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("topProductsByRevenue")]
        public async Task<IActionResult> GetTopProductsByRevenue(int count = 5)
        {
            if (count <= 0) count = 5;

            var data = await _context.Orders
                .Where(o => o.Status != OrderStatus.Canceled)
                .GroupBy(o => o.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalRevenue = g.Sum(o => o.TotalPrice)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(count)
                .Join(_context.Products,
                       orderGroup => orderGroup.ProductId,
                       product => product.Id,
                       (orderGroup, product) => new
                       {
                           ProductName = product.Title,
                           TotalRevenue = orderGroup.TotalRevenue
                       })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("productStatusDistribution")]
        public async Task<IActionResult> GetProductStatusDistribution()
        {
            var data = await _context.Products
               .GroupBy(p => p.Status)
               .Select(g => new
               {
                   Status = g.Key,
                   Count = g.Count()
               })
               .ToListAsync();

            var result = data.Select(d => new {
                StatusName = EnumExtensions.GetDisplayName(d.Status),
                Count = d.Count
            }).ToList();

            return Ok(result);
        }   
    }
}