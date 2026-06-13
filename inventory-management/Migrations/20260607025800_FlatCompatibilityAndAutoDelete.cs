using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace inventory_management.Migrations
{
    /// <inheritdoc />
    public partial class FlatCompatibilityAndAutoDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_item_compatible_models_vehicle_models_vehicle_model_id",
                table: "item_compatible_models");

            migrationBuilder.DropPrimaryKey(
                name: "PK_item_compatible_models",
                table: "item_compatible_models");

            migrationBuilder.DropIndex(
                name: "IX_item_compatible_models_vehicle_model_id",
                table: "item_compatible_models");


            migrationBuilder.RenameColumn(
                name: "vehicle_model_id",
                table: "item_compatible_models",
                newName: "id");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "item_compatible_models",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "brand",
                table: "item_compatible_models",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country_of_origin",
                table: "item_compatible_models",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "manufacturer",
                table: "item_compatible_models",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "model",
                table: "item_compatible_models",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "year_range",
                table: "item_compatible_models",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_item_compatible_models",
                table: "item_compatible_models",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_item_compatible_models_item_id",
                table: "item_compatible_models",
                column: "item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_item_compatible_models",
                table: "item_compatible_models");

            migrationBuilder.DropIndex(
                name: "IX_item_compatible_models_item_id",
                table: "item_compatible_models");

            migrationBuilder.DropColumn(
                name: "brand",
                table: "item_compatible_models");

            migrationBuilder.DropColumn(
                name: "country_of_origin",
                table: "item_compatible_models");

            migrationBuilder.DropColumn(
                name: "manufacturer",
                table: "item_compatible_models");

            migrationBuilder.DropColumn(
                name: "model",
                table: "item_compatible_models");

            migrationBuilder.DropColumn(
                name: "year_range",
                table: "item_compatible_models");


            migrationBuilder.RenameColumn(
                name: "id",
                table: "item_compatible_models",
                newName: "vehicle_model_id");

            migrationBuilder.AlterColumn<int>(
                name: "vehicle_model_id",
                table: "item_compatible_models",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_item_compatible_models",
                table: "item_compatible_models",
                columns: new[] { "item_id", "vehicle_model_id" });

            migrationBuilder.CreateIndex(
                name: "IX_item_compatible_models_vehicle_model_id",
                table: "item_compatible_models",
                column: "vehicle_model_id");

            migrationBuilder.AddForeignKey(
                name: "FK_item_compatible_models_vehicle_models_vehicle_model_id",
                table: "item_compatible_models",
                column: "vehicle_model_id",
                principalTable: "vehicle_models",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
