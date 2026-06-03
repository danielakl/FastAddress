using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastAddress.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameStreetAddressesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename in place to preserve existing rows (the default scaffold was drop + create).
            migrationBuilder.RenameTable(
                name: "street_addresses",
                schema: "address",
                newName: "street_address_results",
                newSchema: "address");

            migrationBuilder.Sql(
                "ALTER TABLE address.street_address_results " +
                "RENAME CONSTRAINT \"PK_street_addresses\" TO \"PK_street_address_results\";");

            migrationBuilder.RenameIndex(
                name: "IX_street_addresses_GooglePlaceId",
                newName: "IX_street_address_results_GooglePlaceId",
                schema: "address",
                table: "street_address_results");

            migrationBuilder.RenameIndex(
                name: "IX_street_addresses_Location",
                newName: "IX_street_address_results_Location",
                schema: "address",
                table: "street_address_results");

            migrationBuilder.RenameIndex(
                name: "IX_street_addresses_SearchText",
                newName: "IX_street_address_results_SearchText",
                schema: "address",
                table: "street_address_results");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_street_address_results_GooglePlaceId",
                newName: "IX_street_addresses_GooglePlaceId",
                schema: "address",
                table: "street_address_results");

            migrationBuilder.RenameIndex(
                name: "IX_street_address_results_Location",
                newName: "IX_street_addresses_Location",
                schema: "address",
                table: "street_address_results");

            migrationBuilder.RenameIndex(
                name: "IX_street_address_results_SearchText",
                newName: "IX_street_addresses_SearchText",
                schema: "address",
                table: "street_address_results");

            migrationBuilder.Sql(
                "ALTER TABLE address.street_address_results " +
                "RENAME CONSTRAINT \"PK_street_address_results\" TO \"PK_street_addresses\";");

            migrationBuilder.RenameTable(
                name: "street_address_results",
                schema: "address",
                newName: "street_addresses",
                newSchema: "address");
        }
    }
}
