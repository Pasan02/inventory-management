using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace inventory_management.Migrations
{
    public partial class AddImageBlobColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "image",
                table: "part_types",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "logo",
                table: "vehicle_manufacturers",
                type: "bytea",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image",
                table: "part_types");

            migrationBuilder.DropColumn(
                name: "logo",
                table: "vehicle_manufacturers");
        }
    }
}
