using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    [Authorize]
    public class ProductsController(
        ApplicationDbContext context) : Controller
    {
        private static readonly string[] AllowedCategories =
        {
            "Bebida",
            "Postre",
            "Snack"
        };

        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string? search,
            string? category)
        {
            IQueryable<Product> products =
                context.Products.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string text = search.Trim().ToLower();

                products = products.Where(p =>
                    p.Name.ToLower().Contains(text) ||
                    (p.Description != null &&
                     p.Description.ToLower().Contains(text)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                products = products.Where(
                    p => p.Category == category);
            }

            ViewBag.Search = search;
            ViewBag.Category = category;

            ViewBag.Categories = AllowedCategories;

            List<Product> result = await products
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();

            return View(result);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            Product? product =
                await context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Product product)
        {
            PrepareProduct(product);
            ValidateCategory(product);

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            context.Products.Add(product);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            Product? product =
                await context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(
            int id,
            Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            PrepareProduct(product);
            ValidateCategory(product);

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            Product? existingProduct =
                await context.Products.FindAsync(id);

            if (existingProduct == null)
            {
                return NotFound();
            }

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.Stock = product.Stock;
            existingProduct.MinimumStock = product.MinimumStock;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.Category = product.Category;
            existingProduct.IsAvailable = product.IsAvailable;
            existingProduct.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> LowStock()
        {
            List<Product> products = await context.Products
                .AsNoTracking()
                .Where(p => p.Stock <= p.MinimumStock)
                .OrderBy(p => p.Stock)
                .ThenBy(p => p.Name)
                .ToListAsync();

            return View(products);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            Product? product =
                await context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Product? product =
                await context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            bool hasSales = await context.SaleDetails
                .AnyAsync(detail => detail.ProductId == id);

            if (hasSales)
            {
                product.IsAvailable = false;
                product.UpdatedAt = DateTime.UtcNow;

                TempData["Success"] =
                    "El producto fue desactivado porque tiene ventas registradas.";
            }
            else
            {
                context.Products.Remove(product);

                TempData["Success"] =
                    "Producto eliminado correctamente.";
            }

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private static void PrepareProduct(Product product)
        {
            product.Name = product.Name?.Trim() ?? string.Empty;
            product.Description =
                product.Description?.Trim() ?? string.Empty;
            product.ImageUrl = string.IsNullOrWhiteSpace(product.ImageUrl)
                ? null
                : product.ImageUrl.Trim();
            product.Category = NormalizeCategory(product.Category);

            if (product.Stock == 0)
            {
                product.IsAvailable = false;
            }
        }

        private void ValidateCategory(Product product)
        {
            if (product.Category == null ||
                !AllowedCategories.Contains(product.Category))
            {
                ModelState.AddModelError(
                    nameof(Product.Category),
                    "Selecciona Bebida, Postre o Snack.");
            }
        }

        private static string? NormalizeCategory(string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return null;
            }

            string value = category.Trim();

            if (value.StartsWith("bebida", StringComparison.OrdinalIgnoreCase))
                return "Bebida";

            if (value.StartsWith("postre", StringComparison.OrdinalIgnoreCase))
                return "Postre";

            if (value.StartsWith("snack", StringComparison.OrdinalIgnoreCase))
                return "Snack";

            return value;
        }
    }
}
