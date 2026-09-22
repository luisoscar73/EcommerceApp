using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SaleId { get; set; }

        public Sale? Sale { get; set; }

        [Required]
        [MaxLength(30)]
        public string Method { get; set; } = "Efectivo";

        [MaxLength(100)]
        public string? Reference { get; set; }

        public decimal GrossAmount { get; set; }

        public decimal CommissionRate { get; set; }

        public decimal CommissionAmount { get; set; }

        public decimal NetAmount { get; set; }

        [Required]
        [MaxLength(30)]
        public string ReconciliationStatus { get; set; }
            = "Pendiente";

        public DateTime PaidAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReconciledAt { get; set; }
    }
}
