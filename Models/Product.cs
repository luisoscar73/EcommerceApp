using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Producto")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        [Display(Name = "Descripción")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 999999.99)]
        [Display(Name = "Precio de venta")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        [Display(Name = "Cantidad disponible")]
        public int Stock { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "Stock mínimo")]
        public int MinimumStock { get; set; } = 5;

        [Display(Name = "Imagen")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Categoría")]
        public string? Category { get; set; }

        [Display(Name = "Disponible para la venta")]
        public bool IsAvailable { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}