using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class SavedCartItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SavedCartId { get; set; }

        public SavedCart? SavedCart { get; set; }

        [Required]
        public int ProductId { get; set; }

        public Product? Product { get; set; }

        [Required]
        [MaxLength(100)]
        public string ProductName { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
