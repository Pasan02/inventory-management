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
        public DbSet<ItemCompatibleModel> ItemCompatibleModels { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Constraint: Unique Item Definition (including SecretPriceCode)
            modelBuilder.Entity<Item>()
                .HasIndex(i => new { i.PartTypeId, i.VehicleModelId, i.PartBrandId, i.CountryOfOrigin, i.SecretPriceCode })
                .HasDatabaseName("IX_items_definition_unique")
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

            // Relationship configuration for compatibility
            modelBuilder.Entity<ItemCompatibleModel>()
                .HasKey(ic => ic.Id);

            modelBuilder.Entity<ItemCompatibleModel>()
                .HasOne(ic => ic.Item)
                .WithMany(i => i.CompatibleModels)
                .HasForeignKey(ic => ic.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

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
