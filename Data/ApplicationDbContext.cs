using EcommerceApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Data
{
    public class ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleDetail> SaleDetails { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<SavedCart> SavedCarts { get; set; }
        public DbSet<SavedCartItem> SavedCartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(s => s.Total)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleDetail>()
                .Property(d => d.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleDetail>()
                .Property(d => d.Subtotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.GrossAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.CommissionRate)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.CommissionAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.NetAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SavedCartItem>()
                .Property(i => i.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Customer)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleDetail>()
                .HasOne(d => d.Sale)
                .WithMany(s => s.Details)
                .HasForeignKey(d => d.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SaleDetail>()
                .HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Payment)
                .WithOne(p => p.Sale)
                .HasForeignKey<Payment>(p => p.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SavedCart>()
                .HasOne(c => c.Customer)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SavedCartItem>()
                .HasOne(i => i.SavedCart)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.SavedCartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SavedCartItem>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SavedCart>()
                .HasIndex(c => new { c.UserId, c.Status });
        }
    }
}
