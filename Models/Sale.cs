using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class Sale
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? Customer { get; set; }

        public DateTime SaleDate { get; set; }
            = DateTime.UtcNow;

        [Range(0, 9999999)]
        public decimal Total { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Pendiente";

        public List<SaleDetail> Details { get; set; }
            = new List<SaleDetail>();
    }
}