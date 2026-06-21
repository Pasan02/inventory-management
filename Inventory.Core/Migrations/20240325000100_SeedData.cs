using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace inventory_management.Migrations
{
    public partial class SeedData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO part_brands (id, name)
                VALUES (1, 'Denso'), (2, 'Bosch')
                ON CONFLICT DO NOTHING;

                INSERT INTO part_types (id, name)
                VALUES (1, 'Compressor'), (2, 'Condenser')
                ON CONFLICT DO NOTHING;

                INSERT INTO racks (id, location_code)
                VALUES (1, 'A-01'), (2, 'B-05')
                ON CONFLICT DO NOTHING;

                INSERT INTO vehicle_manufacturers (id, name)
                VALUES (1, 'Toyota'), (2, 'Ford')
                ON CONFLICT DO NOTHING;

                INSERT INTO vehicle_models (id, manufacturer_id, name, year_range)
                VALUES (1, 1, 'Corolla', '2010-2015'), (2, 2, 'Focus', '2012-2018')
                ON CONFLICT DO NOTHING;

                INSERT INTO items (id, barcode, country_of_origin, description, image_path, low_stock_threshold, part_brand_id, part_type_id, rack_id, vehicle_model_id)
                VALUES (1, 'ITEM-0001', 'Japan', 'Placeholder compressor', NULL, 5, 1, 1, 1, 1)
                ON CONFLICT DO NOTHING;

                INSERT INTO stock (id, item_id, last_updated, quantity)
                VALUES (1, 1, TIMESTAMP '2024-01-01 00:00:00', 10)
                ON CONFLICT DO NOTHING;

                INSERT INTO stock_transactions (id, action_type, checksum_hash, item_id, machine_name, quantity_change, timestamp)
                VALUES (1, 'IN', 'SEED', 1, 'SEED', 10, TIMESTAMP '2024-01-01 00:00:00')
                ON CONFLICT DO NOTHING;

                INSERT INTO user_accounts (id, created_utc, is_active, password_hash, password_salt, username)
                VALUES (1, TIMESTAMP '2024-01-01 00:00:00', TRUE, '042D0C9E0A2D1DB965D318395E299089255C4E75732E9715E95D176FB920BA36', '234857E0303BC7BFC959FA34754C90AB', 'admin')
                ON CONFLICT DO NOTHING;

                INSERT INTO user_login_audits (id, failure_reason, machine_name, success, timestamp, user_account_id)
                VALUES (1, NULL, 'SEED', TRUE, TIMESTAMP '2024-01-01 00:00:00', 1)
                ON CONFLICT DO NOTHING;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "stock_transactions",
                keyColumn: "id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "stock",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "user_login_audits",
                keyColumn: "id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "items",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "user_accounts",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "part_brands",
                keyColumn: "id",
                keyValue: 1);
            migrationBuilder.DeleteData(
                table: "part_brands",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "part_types",
                keyColumn: "id",
                keyValue: 1);
            migrationBuilder.DeleteData(
                table: "part_types",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "racks",
                keyColumn: "id",
                keyValue: 1);
            migrationBuilder.DeleteData(
                table: "racks",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "vehicle_models",
                keyColumn: "id",
                keyValue: 1);
            migrationBuilder.DeleteData(
                table: "vehicle_models",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "vehicle_manufacturers",
                keyColumn: "id",
                keyValue: 1);
            migrationBuilder.DeleteData(
                table: "vehicle_manufacturers",
                keyColumn: "id",
                keyValue: 2);
        }
    }
}
