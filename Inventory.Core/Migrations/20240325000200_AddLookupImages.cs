using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace inventory_management.Migrations
{
    public partial class AddLookupImages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_path",
                table: "part_types",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_path",
                table: "vehicle_manufacturers",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_path",
                table: "part_types");

            migrationBuilder.DropColumn(
                name: "logo_path",
                table: "vehicle_manufacturers");
        }
    }
}
