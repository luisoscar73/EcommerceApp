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

        private async Task SaveCartAsync(List<CartItem> cart)
        {
            HttpContext.Session.SetObject(CartKey, cart);

            string? userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;

            SavedCart? savedCart = await _context.SavedCarts
                .Include(c => c.Items)
                .Where(c => c.UserId == userId &&
                    (c.Status == "Activo" ||
                     c.Status == "Abandonado"))
                .OrderByDescending(c => c.UpdatedAt)
                .FirstOrDefaultAsync();

            if (cart.Count == 0)
            {
                if (savedCart != null)
                {
                    savedCart.Status = "Vacio";
                    savedCart.UpdatedAt = now;
                    savedCart.AbandonedAt = null;
                    _context.SavedCartItems.RemoveRange(savedCart.Items);
                    await _context.SaveChangesAsync();
                }

                return;
            }

            if (savedCart == null)
            {
                savedCart = new SavedCart
                {
                    UserId = userId,
                    Status = "Activo",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.SavedCarts.Add(savedCart);
            }
            else
            {
                savedCart.Status = "Activo";
                savedCart.UpdatedAt = now;
                savedCart.AbandonedAt = null;
                _context.SavedCartItems.RemoveRange(savedCart.Items);
            }

            savedCart.Items = cart.Select(item => new SavedCartItem
            {
                ProductId = item.ProductId,
                ProductName = item.Name,
                UnitPrice = item.Price,
                Quantity = item.Quantity
            }).ToList();

            await _context.SaveChangesAsync();
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

            await SaveCartAsync(cart);

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
                    await SaveCartAsync(cart);
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
        public async Task<IActionResult> Decrease(int productId)
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

                await SaveCartAsync(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int productId)
        {
            List<CartItem> cart = GetCart();

            CartItem? item = cart
                .FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
            {
                cart.Remove(item);
                await SaveCartAsync(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(
            string billingName,
            string taxId,
            string paymentMethod,
            string? paymentReference)
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

            billingName = billingName?.Trim() ?? string.Empty;
            taxId = taxId?.Trim() ?? string.Empty;
            paymentMethod = paymentMethod?.Trim() ?? string.Empty;
            paymentReference = paymentReference?.Trim();

            string[] allowedMethods = { "Efectivo", "QR", "Tarjeta" };

            if (billingName.Length < 3 || taxId.Length < 3)
            {
                TempData["Error"] =
                    "Ingresa el nombre de facturación y el NIT o CI.";
                return RedirectToAction(nameof(Index));
            }

            if (!allowedMethods.Contains(paymentMethod))
            {
                TempData["Error"] = "Selecciona un método de pago válido.";
                return RedirectToAction(nameof(Index));
            }

            if (paymentMethod != "Efectivo" &&
                string.IsNullOrWhiteSpace(paymentReference))
            {
                TempData["Error"] =
                    "Ingresa la referencia del pago QR o tarjeta.";
                return RedirectToAction(nameof(Index));
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                DateTime now = DateTime.UtcNow;

                Sale sale = new Sale
                {
                    UserId = user.Id,
                    SaleDate = now,
                    IssuedAt = now,
                    BillingName = billingName,
                    TaxId = taxId,
                    Status = "Completada",
                    Total = 0
                };

                foreach (CartItem item in cart)
                {
                    Product? product = await _context.Products
                        .FirstOrDefaultAsync(
                            p => p.Id == item.ProductId);

                    if (product == null || !product.IsAvailable)
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

                    decimal subtotal = product.Price * item.Quantity;

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

                decimal commissionRate = paymentMethod switch
                {
                    "QR" => 1m,
                    "Tarjeta" => 3m,
                    _ => 0m
                };

                decimal commissionAmount =
                    Math.Round(sale.Total * commissionRate / 100m, 2);

                sale.Payment = new Payment
                {
                    Method = paymentMethod,
                    Reference = paymentReference,
                    GrossAmount = sale.Total,
                    CommissionRate = commissionRate,
                    CommissionAmount = commissionAmount,
                    NetAmount = sale.Total - commissionAmount,
                    ReconciliationStatus = paymentMethod == "Efectivo"
                        ? "Conciliado"
                        : "Pendiente",
                    PaidAt = now,
                    ReconciledAt = paymentMethod == "Efectivo"
                        ? now
                        : null
                };

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                sale.InvoiceNumber =
                    $"FAC-{now.AddHours(-4):yyyyMMdd}-{sale.Id:000000}";

                SavedCart? savedCart = await _context.SavedCarts
                    .Where(c => c.UserId == user.Id &&
                        (c.Status == "Activo" ||
                         c.Status == "Abandonado"))
                    .OrderByDescending(c => c.UpdatedAt)
                    .FirstOrDefaultAsync();

                if (savedCart != null)
                {
                    savedCart.Status = "Convertido";
                    savedCart.ConvertedAt = now;
                    savedCart.UpdatedAt = now;
                    savedCart.AbandonedAt = null;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                HttpContext.Session.Remove(CartKey);

                TempData["Success"] =
                    $"Compra {sale.InvoiceNumber} realizada correctamente.";

                return RedirectToAction(
                    "Details",
                    "Sales",
                    new { id = sale.Id });
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
