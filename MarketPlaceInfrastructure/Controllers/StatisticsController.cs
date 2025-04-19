using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketPlaceDomain.Model;
using MarketPlaceInfrastructure;
using MarketPlaceInfrastructure.Models;

public class StatisticsController : Controller
{
    private readonly OlxContext _context;

    public StatisticsController(OlxContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }
}
