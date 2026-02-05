using System;
using inventory_management.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace inventory_management.Migrations
{
    [DbContext(typeof(InventoryDbContext))]
    partial class InventoryDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("inventory_management.Data.Entities.PartBrand", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("name");

                    b.HasKey("Id");

                    b.ToTable("part_brands");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Name = "Denso"
                        },
                        new
                        {
                            Id = 2,
                            Name = "Bosch"
                        });
                });

            modelBuilder.Entity("inventory_management.Data.Entities.PartType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("name");

                    b.HasKey("Id");

                    b.ToTable("part_types");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Name = "Compressor"
                        },
                        new
                        {
                            Id = 2,
                            Name = "Condenser"
                        });
                });

            modelBuilder.Entity("inventory_management.Data.Entities.Rack", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("LocationCode")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("location_code");

                    b.HasKey("Id");

                    b.ToTable("racks");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            LocationCode = "A-01"
                        },
                        new
                        {
                            Id = 2,
                            LocationCode = "B-05"
                        });
                });

            modelBuilder.Entity("inventory_management.Data.Entities.UserAccount", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedUtc")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("created_utc");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean")
                        .HasColumnName("is_active");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)")
                        .HasColumnName("password_hash");

                    b.Property<string>("PasswordSalt")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)")
                        .HasColumnName("password_salt");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("username");

                    b.HasKey("Id");

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("user_accounts");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            CreatedUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                            IsActive = true,
                            PasswordHash = "D6ED7D5BE34EB80E6E4FB95339FD24752634F6FF637FD574395D9C8326B81823",
                            PasswordSalt = "234857E0303BC7BFC959FA34754C90AB",
                            Username = "admin"
                        });
                });

            modelBuilder.Entity("inventory_management.Data.Entities.VehicleManufacturer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("name");

                    b.HasKey("Id");

                    b.ToTable("vehicle_manufacturers");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Name = "Toyota"
                        },
                        new
                        {
                            Id = 2,
                            Name = "Ford"
                        });
                });

            modelBuilder.Entity("inventory_management.Data.Entities.VehicleModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("VehicleManufacturerId")
                        .HasColumnType("integer")
                        .HasColumnName("manufacturer_id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("name");

                    b.Property<string>("YearRange")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("year_range");

                    b.HasKey("Id");

                    b.HasIndex("VehicleManufacturerId");

                    b.ToTable("vehicle_models");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            VehicleManufacturerId = 1,
                            Name = "Corolla",
                            YearRange = "2010-2015"
                        },
                        new
                        {
                            Id = 2,
                            VehicleManufacturerId = 2,
                            Name = "Focus",
                            YearRange = "2012-2018"
                        });
                });

            modelBuilder.Entity("inventory_management.Data.Entities.Item", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Barcode")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("barcode");

                    b.Property<string>("CountryOfOrigin")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("country_of_origin");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<string>("ImagePath")
                        .HasColumnType("text")
                        .HasColumnName("image_path");

                    b.Property<int>("LowStockThreshold")
                        .HasColumnType("integer")
                        .HasColumnName("low_stock_threshold");

                    b.Property<int>("PartBrandId")
                        .HasColumnType("integer")
                        .HasColumnName("part_brand_id");

                    b.Property<int>("PartTypeId")
                        .HasColumnType("integer")
                        .HasColumnName("part_type_id");

                    b.Property<int?>("RackId")
                        .HasColumnType("integer")
                        .HasColumnName("rack_id");

                    b.Property<int>("VehicleModelId")
                        .HasColumnType("integer")
                        .HasColumnName("vehicle_model_id");

                    b.HasKey("Id");

                    b.HasIndex("Barcode")
                        .IsUnique();

                    b.HasIndex("PartBrandId");

                    b.HasIndex("PartTypeId");

                    b.HasIndex("RackId");

                    b.HasIndex("VehicleModelId");

                    b.HasIndex("PartTypeId", "VehicleModelId", "PartBrandId", "CountryOfOrigin")
                        .IsUnique();

                    b.ToTable("items");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Barcode = "ITEM-0001",
                            CountryOfOrigin = "Japan",
                            Description = "Placeholder compressor",
                            ImagePath = (string)null,
                            LowStockThreshold = 5,
                            PartBrandId = 1,
                            PartTypeId = 1,
                            RackId = 1,
                            VehicleModelId = 1
                        });
                });

            modelBuilder.Entity("inventory_management.Data.Entities.Stock", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("ItemId")
                        .HasColumnType("integer")
                        .HasColumnName("item_id");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("last_updated");

                    b.Property<int>("Quantity")
                        .HasColumnType("integer")
                        .HasColumnName("quantity");

                    b.HasKey("Id");

                    b.HasIndex("ItemId")
                        .IsUnique();

                    b.ToTable("stock");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            ItemId = 1,
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                            Quantity = 10
                        });
                });

            modelBuilder.Entity("inventory_management.Data.Entities.StockTransaction", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("ActionType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("action_type");

                    b.Property<string>("ChecksumHash")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("checksum_hash");

                    b.Property<int>("ItemId")
                        .HasColumnType("integer")
                        .HasColumnName("item_id");

                    b.Property<string>("MachineName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("machine_name");

                    b.Property<int>("QuantityChange")
                        .HasColumnType("integer")
                        .HasColumnName("quantity_change");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("timestamp");

                    b.HasKey("Id");

                    b.HasIndex("ItemId");

                    b.ToTable("stock_transactions");

                    b.HasData(
                        new
                        {
                            Id = 1L,
                            ActionType = "IN",
                            ChecksumHash = "SEED",
                            ItemId = 1,
                            MachineName = "SEED",
                            QuantityChange = 10,
                            Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)
                        });
                });

            modelBuilder.Entity("inventory_management.Data.Entities.UserLoginAudit", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("FailureReason")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("failure_reason");

                    b.Property<string>("MachineName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("machine_name");

                    b.Property<bool>("Success")
                        .HasColumnType("boolean")
                        .HasColumnName("success");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("timestamp");

                    b.Property<int?>("UserAccountId")
                        .HasColumnType("integer")
                        .HasColumnName("user_account_id");

                    b.HasKey("Id");

                    b.HasIndex("UserAccountId");

                    b.ToTable("user_login_audits");

                    b.HasData(
                        new
                        {
                            Id = 1L,
                            FailureReason = (string)null,
                            MachineName = "SEED",
                            Success = true,
                            Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                            UserAccountId = 1
                        });
                });

            modelBuilder.Entity("inventory_management.Data.Entities.VehicleModel", b =>
                {
                    b.HasOne("inventory_management.Data.Entities.VehicleManufacturer", "Manufacturer")
                        .WithMany()
                        .HasForeignKey("VehicleManufacturerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Manufacturer");
                });

            modelBuilder.Entity("inventory_management.Data.Entities.Item", b =>
                {
                    b.HasOne("inventory_management.Data.Entities.PartBrand", "PartBrand")
                        .WithMany()
                        .HasForeignKey("PartBrandId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("inventory_management.Data.Entities.PartType", "PartType")
                        .WithMany()
                        .HasForeignKey("PartTypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("inventory_management.Data.Entities.Rack", "Rack")
                        .WithMany()
                        .HasForeignKey("RackId");

                    b.HasOne("inventory_management.Data.Entities.VehicleModel", "VehicleModel")
                        .WithMany()
                        .HasForeignKey("VehicleModelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PartBrand");
                    b.Navigation("PartType");
                    b.Navigation("Rack");
                    b.Navigation("VehicleModel");
                });

            modelBuilder.Entity("inventory_management.Data.Entities.Stock", b =>
                {
                    b.HasOne("inventory_management.Data.Entities.Item", "Item")
                        .WithOne("Stock")
                        .HasForeignKey("inventory_management.Data.Entities.Stock", "ItemId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Item");
                });

            modelBuilder.Entity("inventory_management.Data.Entities.StockTransaction", b =>
                {
                    b.HasOne("inventory_management.Data.Entities.Item", "Item")
                        .WithMany()
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Item");
                });

            modelBuilder.Entity("inventory_management.Data.Entities.UserLoginAudit", b =>
                {
                    b.HasOne("inventory_management.Data.Entities.UserAccount", "UserAccount")
                        .WithMany()
                        .HasForeignKey("UserAccountId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("UserAccount");
                });
        }
    }
}
