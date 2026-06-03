using Microsoft.EntityFrameworkCore.Migrations;

using NodaTime;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FastAddress.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStreetAddressQuery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Instant>(
                name: "LastRefreshed",
                schema: "address",
                table: "street_address_results",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(Instant),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateTable(
                name: "street_address_queries",
                schema: "address",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Added = table.Column<Instant>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    Query = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    LastRefreshed = table.Column<Instant>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_street_address_queries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_street_address_queries_Query",
                schema: "address",
                table: "street_address_queries",
                column: "Query",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "street_address_queries",
                schema: "address");

            migrationBuilder.AlterColumn<Instant>(
                name: "LastRefreshed",
                schema: "address",
                table: "street_address_results",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(Instant),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");
        }
    }
}
