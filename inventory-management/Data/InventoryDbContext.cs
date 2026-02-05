using inventory_management.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace inventory_management.Data
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
        {
        }

        public DbSet<PartType> PartTypes { get; set; }
        public DbSet<PartBrand> PartBrands { get; set; }
        public DbSet<VehicleManufacturer> Manufacturers { get; set; }
        public DbSet<VehicleModel> Models { get; set; }
        public DbSet<Rack> Racks { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<StockTransaction> Transactions { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<UserLoginAudit> UserLoginAudits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Constraint: Unique Item Definition
            modelBuilder.Entity<Item>()
                .HasIndex(i => new { i.PartTypeId, i.VehicleModelId, i.PartBrandId, i.CountryOfOrigin })
                .IsUnique();

            // Constraint: Barcode is unique
            modelBuilder.Entity<Item>()
                .HasIndex(i => i.Barcode)
                .IsUnique();

            // Constraint: Item has exactly one Stock record
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Stock)
                .WithOne(s => s.Item)
                .HasForeignKey<Stock>(s => s.ItemId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent accidental deletions

            modelBuilder.Entity<UserAccount>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<UserLoginAudit>()
                .HasOne(a => a.UserAccount)
                .WithMany()
                .HasForeignKey(a => a.UserAccountId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
