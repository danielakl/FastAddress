using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastAddress.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLastRefreshedIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_street_address_results_LastRefreshed",
                schema: "address",
                table: "street_address_results",
                column: "LastRefreshed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_street_address_results_LastRefreshed",
                schema: "address",
                table: "street_address_results");
        }
    }
}
