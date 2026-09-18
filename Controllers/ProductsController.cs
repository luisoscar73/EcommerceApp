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

            ViewBag.Categories = await context.Products
                .Where(p => p.Category != null &&
                            p.Category != "")
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

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
    }
}