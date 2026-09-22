using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController(
        ApplicationDbContext context,
        PdfReportService pdfReportService) : Controller
    {
        private const int AbandonmentMinutes = 30;

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? from,
            DateTime? to)
        {
            await MarkInactiveCartsAsAbandonedAsync();
            ReportsDashboardViewModel report =
                await BuildReportAsync(from, to);

            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPdf(
            DateTime? from,
            DateTime? to)
        {
            await MarkInactiveCartsAsAbandonedAsync();
            ReportsDashboardViewModel report =
                await BuildReportAsync(from, to);

            byte[] pdf = pdfReportService.GeneratePeriodReport(report);

            string fileName =
                $"reporte-{report.From:yyyyMMdd}-{report.To:yyyyMMdd}-{BoliviaNow():HHmm}.pdf";

            return File(pdf, "application/pdf", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReconcilePayment(
            int id,
            DateTime? from,
            DateTime? to)
        {
            Payment? payment = await context.Payments
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            payment.ReconciliationStatus = "Conciliado";
            payment.ReconciledAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            TempData["Success"] =
                $"El pago #{payment.Id} fue conciliado correctamente.";

            return RedirectToAction(nameof(Index), new
            {
                from = from?.ToString("yyyy-MM-dd"),
                to = to?.ToString("yyyy-MM-dd")
            });
        }

        private async Task MarkInactiveCartsAsAbandonedAsync()
        {
            DateTime now = DateTime.UtcNow;
            DateTime limit = now.AddMinutes(-AbandonmentMinutes);

            List<SavedCart> carts = await context.SavedCarts
                .Where(c => c.Status == "Activo" &&
                    c.UpdatedAt <= limit &&
                    c.Items.Any())
                .ToListAsync();

            if (carts.Count == 0)
            {
                return;
            }

            foreach (SavedCart cart in carts)
            {
                cart.Status = "Abandonado";
                cart.AbandonedAt = now;
            }

            await context.SaveChangesAsync();
        }

        private async Task<ReportsDashboardViewModel> BuildReportAsync(
            DateTime? from,
            DateTime? to)
        {
            DateTime today = BoliviaNow().Date;
            DateTime fromLocal = (from ??
                new DateTime(today.Year, today.Month, 1)).Date;
            DateTime toLocal = (to ?? today).Date;

            if (toLocal < fromLocal)
            {
                (fromLocal, toLocal) = (toLocal, fromLocal);
            }

            DateTime fromUtc = DateTime.SpecifyKind(
                fromLocal.AddHours(4),
                DateTimeKind.Utc);

            DateTime toUtcExclusive = DateTime.SpecifyKind(
                toLocal.AddDays(1).AddHours(4),
                DateTimeKind.Utc);

            List<Sale> sales = await context.Sales
                .AsNoTracking()
                .Where(s => s.SaleDate >= fromUtc &&
                    s.SaleDate < toUtcExclusive)
                .Include(s => s.Customer)
                .Include(s => s.Payment)
                .Include(s => s.Details)
                .ThenInclude(d => d.Product)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            List<BestSellingProductViewModel> bestSellers = sales
                .SelectMany(s => s.Details)
                .GroupBy(d => new
                {
                    d.ProductId,
                    Name = d.Product?.Name ?? "Producto"
                })
                .Select(group => new BestSellingProductViewModel
                {
                    ProductId = group.Key.ProductId,
                    ProductName = group.Key.Name,
                    Quantity = group.Sum(d => d.Quantity),
                    Income = group.Sum(d => d.Subtotal)
                })
                .OrderByDescending(item => item.Quantity)
                .ThenBy(item => item.ProductName)
                .ToList();

            List<Product> lowStockProducts = await context.Products
                .AsNoTracking()
                .Where(p => p.Stock <= p.MinimumStock)
                .OrderBy(p => p.Stock)
                .ThenBy(p => p.Name)
                .ToListAsync();

            List<SavedCart> abandonedCarts = await context.SavedCarts
                .AsNoTracking()
                .Where(c => c.Status == "Abandonado")
                .Include(c => c.Customer)
                .Include(c => c.Items)
                .OrderByDescending(c => c.AbandonedAt)
                .ToListAsync();

            return new ReportsDashboardViewModel
            {
                From = fromLocal,
                To = toLocal,
                GeneratedAt = DateTime.UtcNow,
                Sales = sales,
                BestSellers = bestSellers,
                LowStockProducts = lowStockProducts,
                AbandonedCarts = abandonedCarts,
                Payments = sales
                    .Where(s => s.Payment != null)
                    .Select(s => s.Payment!)
                    .OrderByDescending(p => p.PaidAt)
                    .ToList()
            };
        }

        private static DateTime BoliviaNow()
            => DateTime.UtcNow.AddHours(-4);
    }
}
