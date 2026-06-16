using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace inventory_management.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "part_brands",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_part_brands", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "part_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_part_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "racks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    location_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_racks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_accounts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    password_salt = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_utc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_manufacturers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_manufacturers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_models",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    manufacturer_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    year_range = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_models", x => x.id);
                    table.ForeignKey(
                        name: "FK_vehicle_models_vehicle_manufacturers_manufacturer_id",
                        column: x => x.manufacturer_id,
                        principalTable: "vehicle_manufacturers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    part_type_id = table.Column<int>(type: "integer", nullable: false),
                    vehicle_model_id = table.Column<int>(type: "integer", nullable: false),
                    part_brand_id = table.Column<int>(type: "integer", nullable: false),
                    country_of_origin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    image_path = table.Column<string>(type: "text", nullable: true),
                    low_stock_threshold = table.Column<int>(type: "integer", nullable: false),
                    rack_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_items_part_brands_part_brand_id",
                        column: x => x.part_brand_id,
                        principalTable: "part_brands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_items_part_types_part_type_id",
                        column: x => x.part_type_id,
                        principalTable: "part_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_items_racks_rack_id",
                        column: x => x.rack_id,
                        principalTable: "racks",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_items_vehicle_models_vehicle_model_id",
                        column: x => x.vehicle_model_id,
                        principalTable: "vehicle_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_login_audits",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_account_id = table.Column<int>(type: "integer", nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    machine_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_login_audits", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_login_audits_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "stock",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock", x => x.id);
                    table.ForeignKey(
                        name: "FK_stock_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_transactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    action_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity_change = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    machine_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    checksum_hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_stock_transactions_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_items_barcode",
                table: "items",
                column: "barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_part_brand_id",
                table: "items",
                column: "part_brand_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_part_type_id",
                table: "items",
                column: "part_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_rack_id",
                table: "items",
                column: "rack_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_vehicle_model_id",
                table: "items",
                column: "vehicle_model_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_part_type_id_vehicle_model_id_part_brand_id_country_of_origin",
                table: "items",
                columns: new[] { "part_type_id", "vehicle_model_id", "part_brand_id", "country_of_origin" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_item_id",
                table: "stock",
                column: "item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_item_id",
                table: "stock_transactions",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_username",
                table: "user_accounts",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_login_audits_user_account_id",
                table: "user_login_audits",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_models_manufacturer_id",
                table: "vehicle_models",
                column: "manufacturer_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_transactions");

            migrationBuilder.DropTable(
                name: "stock");

            migrationBuilder.DropTable(
                name: "user_login_audits");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "user_accounts");

            migrationBuilder.DropTable(
                name: "part_brands");

            migrationBuilder.DropTable(
                name: "part_types");

            migrationBuilder.DropTable(
                name: "racks");

            migrationBuilder.DropTable(
                name: "vehicle_models");

            migrationBuilder.DropTable(
                name: "vehicle_manufacturers");
        }
    }
}
