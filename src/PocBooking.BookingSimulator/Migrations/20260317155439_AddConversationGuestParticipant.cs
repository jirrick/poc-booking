using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PocBooking.BookingSimulator.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationGuestParticipant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GuestParticipantId",
                table: "Conversations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_GuestParticipantId",
                table: "Conversations",
                column: "GuestParticipantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Participants_GuestParticipantId",
                table: "Conversations",
                column: "GuestParticipantId",
                principalTable: "Participants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Participants_GuestParticipantId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_GuestParticipantId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "GuestParticipantId",
                table: "Conversations");
        }
    }
}
