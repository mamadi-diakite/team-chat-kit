using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamChat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialChatSchema : ChatMigration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: TablePrefix + "Contacts",
                columns: table => new
                {
                    FirstUserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SecondUserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_" + TablePrefix + "Contacts", x => new { x.FirstUserId, x.SecondUserId });
                });

            migrationBuilder.CreateTable(
                name: TablePrefix + "Invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SenderId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RecipientId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_" + TablePrefix + "Invitations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_" + TablePrefix + "Contacts_SecondUserId",
                table: TablePrefix + "Contacts",
                column: "SecondUserId");

            migrationBuilder.CreateIndex(
                name: "IX_" + TablePrefix + "Invitations_RecipientId_Status",
                table: TablePrefix + "Invitations",
                columns: new[] { "RecipientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_" + TablePrefix + "Invitations_SenderId_RecipientId",
                table: TablePrefix + "Invitations",
                columns: new[] { "SenderId", "RecipientId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: TablePrefix + "Contacts");

            migrationBuilder.DropTable(
                name: TablePrefix + "Invitations");
        }
    }
}
