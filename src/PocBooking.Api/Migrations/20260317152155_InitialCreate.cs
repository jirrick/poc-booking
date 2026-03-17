using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PocBooking.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuestMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookingGuestId = table.Column<string>(type: "TEXT", nullable: false),
                    InternalGuestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationInbox",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NotificationUuid = table.Column<Guid>(type: "TEXT", nullable: false),
                    MessageId = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationInbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NotificationInboxId = table.Column<int>(type: "INTEGER", nullable: false),
                    InternalReservationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InternalGuestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReservationMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookingReservationId = table.Column<string>(type: "TEXT", nullable: false),
                    InternalReservationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestMappings_BookingGuestId",
                table: "GuestMappings",
                column: "BookingGuestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationInbox_MessageId",
                table: "NotificationInbox",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationInbox_NotificationUuid",
                table: "NotificationInbox",
                column: "NotificationUuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedMessages_NotificationInboxId",
                table: "ProcessedMessages",
                column: "NotificationInboxId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationMappings_BookingReservationId",
                table: "ReservationMappings",
                column: "BookingReservationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestMappings");

            migrationBuilder.DropTable(
                name: "NotificationInbox");

            migrationBuilder.DropTable(
                name: "ProcessedMessages");

            migrationBuilder.DropTable(
                name: "ReservationMappings");
        }
    }
}
