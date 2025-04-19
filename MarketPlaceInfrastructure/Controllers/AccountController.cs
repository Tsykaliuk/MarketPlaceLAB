using MarketPlaceDomain.Model;
using MarketPlaceInfrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly OlxContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(SignInManager<ApplicationUser> signInManager, OlxContext context, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        var roles = await _userManager.GetRolesAsync(user);

        var myProducts = await _context.Products
                           .Where(p => p.UserId == userId)
                           .Include(p => p.Images)
                           .Include(p => p.Category)
                           .OrderByDescending(p => p.CreatedAt)
                           .ToListAsync();

        var myOrders = await _context.Orders
                    .Where(o => o.UserId == userId)
                    .Include(o => o.Product)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

        var incomingOrders = await _context.Orders
            .Where(o => o.Product.UserId == userId)
            .Include(o => o.Product)
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var vm = new ProfileViewModel
        {
            User = user,
            MyProducts = myProducts,
            MyOrders = myOrders,
            IncomingOrders = incomingOrders,
            Roles = roles.ToList()
        };

        return View(vm);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
