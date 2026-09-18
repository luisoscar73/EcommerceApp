using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class SaleDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SaleId { get; set; }

        public Sale? Sale { get; set; }

        [Required]
        public int ProductId { get; set; }

        public Product? Product { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0.01, 999999.99)]
        public decimal UnitPrice { get; set; }

        [Range(0.01, 9999999)]
        public decimal Subtotal { get; set; }
    }
}