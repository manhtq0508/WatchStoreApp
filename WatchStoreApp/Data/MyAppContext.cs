using Microsoft.EntityFrameworkCore;
using WatchStoreApp.Models;

namespace WatchStoreApp.Data
{
    public class MyAppContext : DbContext
    {
        public MyAppContext(DbContextOptions<MyAppContext> options)
            : base(options)
        {
        }
        public DbSet<Brand> Brands { get; set; } = null!;
        public DbSet<Coupon> Coupons { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<MechanicalWatch> MechanicalWatches { get; set; } = null!;
        public DbSet<SmartWatch> SmartWatches { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<ImportBill> ImportBills { get; set; } = null!;
        public DbSet<ImportDetail> ImportDetails { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<InvoiceDetail> InvoiceDetails { get; set; } = null!;
        public DbSet<Setting> Settings { get; set; } = null!;
        public DbSet<Cart> Carts { get; set; } = null!;

        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
            builder.Properties<decimal>().HavePrecision(18, 2);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Setting>().HasData(
                new Setting
                {
                    SettingId = 1,
                    ShippingFee = 500000,
                    Banner1_1 = "/images/banner/banner1_1.jpg",
                    Banner1_2 = "/images/banner/banner1_2.png",
                    Banner1_3 = "/images/banner/banner1_3.jpg",
                    Banner2 = "/images/banner/banner2.jpg",
                    Banner3 = "/images/banner/banner3.jpg",
                    Banner4 = "/images/banner/banner4.jpg"
                }
            );

            modelBuilder.Entity<Product>()
                .HasOne(p => p.MechanicalWatch)
                .WithOne(m => m.Product)
                .HasForeignKey<MechanicalWatch>(m => m.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.SmartWatch)
                .WithOne(s => s.Product)
                .HasForeignKey<SmartWatch>(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ImportBill>()
                .HasMany(b => b.ImportDetails)
                .WithOne(d => d.ImportBill)
                .HasForeignKey(d => d.ImportBillId);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.ImportDetails)
                .WithOne(d => d.Product)
                .HasForeignKey(d => d.ProductId);

            modelBuilder.Entity<Employee>()
                .HasMany(e => e.ImportBills)
                .WithOne(b => b.Employee)
                .HasForeignKey(b => b.EmployeeId);

            modelBuilder.Entity<Invoice>()
                .HasMany(i => i.InvoiceDetails)
                .WithOne(d => d.Invoice)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.InvoiceDetails)
                .WithOne(d => d.Product)
                .HasForeignKey(d => d.ProductId);

            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Invoices)
                .WithOne(i => i.Customer)
                .HasForeignKey(i => i.CustomerId);

            modelBuilder.Entity<Coupon>()
                .HasMany(c => c.Invoices)
                .WithOne(i => i.Coupon)
                .HasForeignKey(i => i.CouponId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Cart>()
                .HasIndex(c => new { c.CustomerId, c.ProductId })
                .IsUnique();

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Customer)
                .WithMany(cu => cu.Carts)
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Product)
                .WithMany(p => p.Carts)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}