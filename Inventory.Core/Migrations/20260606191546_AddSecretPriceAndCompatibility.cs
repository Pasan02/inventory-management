using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace inventory_management.Migrations
{
    /// <inheritdoc />
    public partial class AddSecretPriceAndCompatibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_items_part_type_id",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_part_type_id_vehicle_model_id_part_brand_id_country_of",
                table: "items");





            migrationBuilder.AlterColumn<DateTime>(
                name: "timestamp",
                table: "user_login_audits",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_utc",
                table: "user_accounts",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "timestamp",
                table: "stock_transactions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_updated",
                table: "stock",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");



            migrationBuilder.AddColumn<DateTime>(
                name: "registered_date",
                table: "items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "secret_price_code",
                table: "items",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "item_compatible_models",
                columns: table => new
                {
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    vehicle_model_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_compatible_models", x => new { x.item_id, x.vehicle_model_id });
                    table.ForeignKey(
                        name: "FK_item_compatible_models_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_item_compatible_models_vehicle_models_vehicle_model_id",
                        column: x => x.vehicle_model_id,
                        principalTable: "vehicle_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_items_definition_unique",
                table: "items",
                columns: new[] { "part_type_id", "vehicle_model_id", "part_brand_id", "country_of_origin", "secret_price_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_compatible_models_vehicle_model_id",
                table: "item_compatible_models",
                column: "vehicle_model_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_compatible_models");

            migrationBuilder.DropIndex(
                name: "IX_items_definition_unique",
                table: "items");

            migrationBuilder.DropColumn(
                name: "registered_date",
                table: "items");

            migrationBuilder.DropColumn(
                name: "secret_price_code",
                table: "items");

            migrationBuilder.AlterColumn<DateTime>(
                name: "timestamp",
                table: "user_login_audits",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_utc",
                table: "user_accounts",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "timestamp",
                table: "stock_transactions",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_updated",
                table: "stock",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateIndex(
                name: "IX_items_part_type_id",
                table: "items",
                column: "part_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_part_type_id_vehicle_model_id_part_brand_id_country_of",
                table: "items",
                columns: new[] { "part_type_id", "vehicle_model_id", "part_brand_id", "country_of_origin" },
                unique: true);
        }
    }
}
