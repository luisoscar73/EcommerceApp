using EcommerceApp.Data;
using EcommerceApp.Helpers;
using EcommerceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string CartKey = "ShoppingCart";

        public CartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private List<CartItem> GetCart()
        {
            return HttpContext.Session
                .GetObject<List<CartItem>>(CartKey)
                ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetObject(CartKey, cart);
        }

        public IActionResult Index()
        {
            return View(GetCart());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId)
        {
            Product? product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null ||
                !product.IsAvailable ||
                product.Stock <= 0)
            {
                TempData["Error"] =
                    "El producto no está disponible.";

                return RedirectToAction("Index", "Products");
            }

            List<CartItem> cart = GetCart();

            CartItem? item = cart
                .FirstOrDefault(c => c.ProductId == productId);

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = 1,
                    ImageUrl = product.ImageUrl
                });
            }
            else if (item.Quantity < product.Stock)
            {
                item.Quantity++;
            }
            else
            {
                TempData["Error"] =
                    "No existe más stock disponible.";

                return RedirectToAction("Index", "Products");
            }

            SaveCart(cart);

            TempData["Success"] =
                "Producto agregado al carrito.";

            return RedirectToAction("Index", "Products");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Increase(int productId)
        {
            List<CartItem> cart = GetCart();

            CartItem? item = cart
                .FirstOrDefault(c => c.ProductId == productId);

            Product? product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (item != null && product != null)
            {
                if (item.Quantity < product.Stock)
                {
                    item.Quantity++;
                    SaveCart(cart);
                }
                else
                {
                    TempData["Error"] =
                        "No existe más stock disponible.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Decrease(int productId)
        {
            List<CartItem> cart = GetCart();

            CartItem? item = cart
                .FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
            {
                item.Quantity--;

                if (item.Quantity <= 0)
                {
                    cart.Remove(item);
                }

                SaveCart(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId)
        {
            List<CartItem> cart = GetCart();

            CartItem? item = cart
                .FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            List<CartItem> cart = GetCart();

            if (cart.Count == 0)
            {
                TempData["Error"] = "El carrito está vacío.";
                return RedirectToAction(nameof(Index));
            }

            ApplicationUser? user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                Sale sale = new Sale
                {
                    UserId = user.Id,
                    SaleDate = DateTime.UtcNow,
                    Status = "Completada",
                    Total = 0
                };

                foreach (CartItem item in cart)
                {
                    Product? product = await _context.Products
                        .FirstOrDefaultAsync(
                            p => p.Id == item.ProductId);

                    if (product == null ||
                        !product.IsAvailable)
                    {
                        await transaction.RollbackAsync();

                        TempData["Error"] =
                            $"El producto {item.Name} ya no está disponible.";

                        return RedirectToAction(nameof(Index));
                    }

                    if (product.Stock < item.Quantity)
                    {
                        await transaction.RollbackAsync();

                        TempData["Error"] =
                            $"No existe suficiente stock de {product.Name}.";

                        return RedirectToAction(nameof(Index));
                    }

                    decimal subtotal =
                        product.Price * item.Quantity;

                    sale.Details.Add(new SaleDetail
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price,
                        Subtotal = subtotal
                    });

                    sale.Total += subtotal;
                    product.Stock -= item.Quantity;

                    if (product.Stock == 0)
                    {
                        product.IsAvailable = false;
                    }
                }

                _context.Sales.Add(sale);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                HttpContext.Session.Remove(CartKey);

                TempData["Success"] =
                    $"Compra #{sale.Id} realizada correctamente.";

                return RedirectToAction("Index", "Products");
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    "No fue posible completar la compra.";

                return RedirectToAction(nameof(Index));
            }
        }
    }
}