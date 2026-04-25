using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmDefenseHarvestWars.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RewardSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchResults",
                columns: table => new
                {
                    MatchId = table.Column<string>(type: "TEXT", nullable: false),
                    DefenderUserId = table.Column<string>(type: "TEXT", nullable: false),
                    AttackerUserId = table.Column<string>(type: "TEXT", nullable: false),
                    WinnerRole = table.Column<int>(type: "INTEGER", nullable: true),
                    IsAborted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefenderGoldEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    DefenderXpEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    AttackerGoldEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    AttackerXpEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchResults", x => x.MatchId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchResults");
        }
    }
}
