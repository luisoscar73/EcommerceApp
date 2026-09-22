using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApp.Models
{
    public class SavedCart
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? Customer { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Activo";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? AbandonedAt { get; set; }

        public DateTime? ConvertedAt { get; set; }

        public List<SavedCartItem> Items { get; set; }
            = new List<SavedCartItem>();

        [NotMapped]
        public decimal Total => Items.Sum(item =>
            item.UnitPrice * item.Quantity);
    }
}
