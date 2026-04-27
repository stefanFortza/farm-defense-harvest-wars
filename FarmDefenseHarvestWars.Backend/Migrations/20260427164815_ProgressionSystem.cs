using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmDefenseHarvestWars.Backend.Migrations
{
    /// <inheritdoc />
    public partial class ProgressionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Fragments",
                table: "UnitUnlocks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "UnitUnlocks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AttackerDroppedChestJson",
                table: "MatchResults",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefenderDroppedChestJson",
                table: "MatchResults",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChestsJson",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fragments",
                table: "UnitUnlocks");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "UnitUnlocks");

            migrationBuilder.DropColumn(
                name: "AttackerDroppedChestJson",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "DefenderDroppedChestJson",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "ChestsJson",
                table: "AspNetUsers");
        }
    }
}
