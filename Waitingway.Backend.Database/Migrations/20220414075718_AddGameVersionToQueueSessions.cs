using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Waitingway.Backend.Database.Migrations
{
    public partial class AddGameVersionToQueueSessions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GameVersion",
                table: "QueueSessions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameVersion",
                table: "QueueSessions");
        }
    }
}
