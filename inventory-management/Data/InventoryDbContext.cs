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

            modelBuilder.Entity<PartType>().HasData(
                new PartType { Id = 1, Name = "Compressor" },
                new PartType { Id = 2, Name = "Condenser" });

            modelBuilder.Entity<PartBrand>().HasData(
                new PartBrand { Id = 1, Name = "Denso" },
                new PartBrand { Id = 2, Name = "Bosch" });

            modelBuilder.Entity<VehicleManufacturer>().HasData(
                new VehicleManufacturer { Id = 1, Name = "Toyota" },
                new VehicleManufacturer { Id = 2, Name = "Ford" });

            modelBuilder.Entity<VehicleModel>().HasData(
                new VehicleModel { Id = 1, VehicleManufacturerId = 1, Name = "Corolla", YearRange = "2010-2015" },
                new VehicleModel { Id = 2, VehicleManufacturerId = 2, Name = "Focus", YearRange = "2012-2018" });

            modelBuilder.Entity<Rack>().HasData(
                new Rack { Id = 1, LocationCode = "A-01" },
                new Rack { Id = 2, LocationCode = "B-05" });

            modelBuilder.Entity<Item>().HasData(
                new Item
                {
                    Id = 1,
                    Barcode = "ITEM-0001",
                    PartTypeId = 1,
                    VehicleModelId = 1,
                    PartBrandId = 1,
                    CountryOfOrigin = "Japan",
                    Description = "Placeholder compressor",
                    ImagePath = null,
                    LowStockThreshold = 5,
                    RackId = 1
                });

            modelBuilder.Entity<Stock>().HasData(
                new Stock
                {
                    Id = 1,
                    ItemId = 1,
                    Quantity = 10,
                    LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)
                });

            modelBuilder.Entity<StockTransaction>().HasData(
                new StockTransaction
                {
                    Id = 1,
                    ItemId = 1,
                    ActionType = "IN",
                    QuantityChange = 10,
                    Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                    MachineName = "SEED",
                    ChecksumHash = "SEED"
                });

            modelBuilder.Entity<UserAccount>().HasData(
                new UserAccount
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = "D6ED7D5BE34EB80E6E4FB95339FD24752634F6FF637FD574395D9C8326B81823",
                    PasswordSalt = "234857E0303BC7BFC959FA34754C90AB",
                    IsActive = true,
                    CreatedUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)
                });

            modelBuilder.Entity<UserLoginAudit>().HasData(
                new UserLoginAudit
                {
                    Id = 1,
                    UserAccountId = 1,
                    Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                    MachineName = "SEED",
                    Success = true,
                    FailureReason = null
                });
        }
    }
}
