using System;
using Fundoo.User.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fundoo.User.Infrastructure.Persistence.Migrations;

[DbContext(typeof(UserDbContext))]
[Migration("20261125160000_AddUserSessions")]
public sealed class AddUserSessions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UserSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                DeviceName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                RefreshTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                PreviousRefreshTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                PreviousTokenRotatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastUsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                AbsoluteExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserSessions", session => session.Id);
                table.ForeignKey(
                    name: "FK_UserSessions_Users_UserId",
                    column: session => session.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_UserSessions_PreviousRefreshTokenHash",
            table: "UserSessions",
            column: "PreviousRefreshTokenHash");

        migrationBuilder.CreateIndex(
            name: "IX_UserSessions_RefreshTokenHash",
            table: "UserSessions",
            column: "RefreshTokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserSessions_UserId_RevokedAtUtc_ExpiresAtUtc",
            table: "UserSessions",
            columns: new[] { "UserId", "RevokedAtUtc", "ExpiresAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "UserSessions");
}
