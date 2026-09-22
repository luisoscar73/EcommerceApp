namespace EcommerceApp.Models
{
    public class ReportsDashboardViewModel
    {
        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public List<Sale> Sales { get; set; } = new();

        public List<BestSellingProductViewModel> BestSellers { get; set; }
            = new();

        public List<Product> LowStockProducts { get; set; } = new();

        public List<SavedCart> AbandonedCarts { get; set; } = new();

        public List<Payment> Payments { get; set; } = new();

        public decimal GrossIncome => Sales.Sum(sale => sale.Total);

        public decimal TotalCommissions =>
            Payments.Sum(payment => payment.CommissionAmount);

        public decimal NetIncome =>
            GrossIncome - TotalCommissions;

        public int UnitsSold => Sales
            .SelectMany(sale => sale.Details)
            .Sum(detail => detail.Quantity);
    }

    public class BestSellingProductViewModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal Income { get; set; }
    }
}
