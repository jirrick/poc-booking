using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PocBooking.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InternalEnterpriseId",
                table: "ProcessedMessages",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "PropertyMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookingPropertyId = table.Column<string>(type: "TEXT", nullable: false),
                    InternalEnterpriseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyMappings_BookingPropertyId",
                table: "PropertyMappings",
                column: "BookingPropertyId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropertyMappings");

            migrationBuilder.DropColumn(
                name: "InternalEnterpriseId",
                table: "ProcessedMessages");
        }
    }
}
