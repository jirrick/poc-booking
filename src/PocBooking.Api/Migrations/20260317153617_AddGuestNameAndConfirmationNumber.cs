using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PocBooking.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestNameAndConfirmationNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConfirmationNumber",
                table: "ReservationMappings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GuestName",
                table: "GuestMappings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmationNumber",
                table: "ReservationMappings");

            migrationBuilder.DropColumn(
                name: "GuestName",
                table: "GuestMappings");
        }
    }
}
