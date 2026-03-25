using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PocBooking.BookingSimulator.Migrations
{
    /// <inheritdoc />
    public partial class AddTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NoReplyNeeded",
                table: "Conversations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "Messages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NoReplyNeeded",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "Messages");
        }
    }
}

