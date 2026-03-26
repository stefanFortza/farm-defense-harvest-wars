using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmDefenseHarvestWars.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitUnlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnitUnlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitType = table.Column<int>(type: "INTEGER", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitUnlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitUnlocks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnitUnlocks_UserId_Role",
                table: "UnitUnlocks",
                columns: new[] { "UserId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitUnlocks_UserId_Role_UnitType",
                table: "UnitUnlocks",
                columns: new[] { "UserId", "Role", "UnitType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnitUnlocks");
        }
    }
}
