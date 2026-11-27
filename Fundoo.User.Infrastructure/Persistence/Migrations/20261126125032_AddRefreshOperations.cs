using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fundoo.User.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefreshOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PresentedTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EncryptedResponse = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshOperations_UserSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "UserSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshOperations_ExpiresAtUtc",
                table: "RefreshOperations",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshOperations_SessionId_PresentedTokenHash",
                table: "RefreshOperations",
                columns: new[] { "SessionId", "PresentedTokenHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshOperations");
        }
    }
}
