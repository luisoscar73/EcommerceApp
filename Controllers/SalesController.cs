using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SalesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> MyPurchases()
        {
            string? userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            List<Sale> sales = await _context.Sales
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Include(s => s.Details)
                .ThenInclude(d => d.Product)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return View(sales);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            List<Sale> sales = await _context.Sales
                .AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.Details)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return View(sales);
        }

        public async Task<IActionResult> Details(int id)
        {
            Sale? sale = await _context.Sales
                .AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.Details)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null)
            {
                return NotFound();
            }

            string? currentUserId =
                _userManager.GetUserId(User);

            bool isAdmin = User.IsInRole("Admin");

            if (!isAdmin && sale.UserId != currentUserId)
            {
                return Forbid();
            }

            return View(sale);
        }
    }
}